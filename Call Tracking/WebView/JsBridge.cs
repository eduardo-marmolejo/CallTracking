using Microsoft.Web.WebView2.Core;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CallTracking.Datos;

namespace CallTracking.WebView
{
    /// <summary>
    /// Puente entre el JavaScript de la interfaz y la capa Datos.
    /// El JS solo puede pedir las operaciones de la lista blanca; nunca ve SQL ni rutas.
    /// Contrato: { op, requestId, payload } -> { requestId, ok, data | error }.
    /// </summary>
    public sealed class JsBridge
    {
        private const string HostPermitido = "app.local";

        private readonly CoreWebView2 _core;
        private readonly System.Windows.Forms.Control? _uiControl;

        private static JsBridge? _instanciaActual;

        private static JsBridge InstanciaActual => _instanciaActual ?? throw new InvalidOperationException("JsBridge no inicializado");

        public JsBridge(CoreWebView2 core, System.Windows.Forms.Control? uiControl = null)
        {
            _core = core;
            _instanciaActual = this;
            _uiControl = uiControl;
            _core.WebMessageReceived += OnWebMessageReceived;
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Solo la página servida por el host virtual de la app puede usar el puente.
            if (!EsOrigenPermitido(e.Source)) return;

            string? requestId = null;
            try
            {
                string mensajePlano = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(mensajePlano);
                var raiz = doc.RootElement;

                // El JS puede mandar el objeto tal cual o como texto JSON.
                if (raiz.ValueKind == JsonValueKind.String)
                {
                    string? textoInterno = raiz.GetString();
                    if (string.IsNullOrWhiteSpace(textoInterno)) return;
                    using var doc2 = JsonDocument.Parse(textoInterno);
                    raiz = doc2.RootElement.Clone();
                    // Ahora trabajamos con un clone fuera del using
                }

                // Extraer todo con strings/copia antes de salir
                string? reqIdStr = null;
                if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("requestId", out var ridEl) && ridEl.ValueKind == JsonValueKind.String)
                    reqIdStr = ridEl.GetString();

                string opStr = string.Empty;
                if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("op", out var opEl2) && opEl2.ValueKind == JsonValueKind.String)
                    opStr = opEl2.GetString() ?? string.Empty;

                string payloadJsonStr = "null";
                if (raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("payload", out var pEl))
                {
                    if (pEl.ValueKind == JsonValueKind.Undefined || pEl.ValueKind == JsonValueKind.Null)
                        payloadJsonStr = "null";
                    else
                        payloadJsonStr = pEl.GetRawText();
                }

                requestId = reqIdStr;

                if (string.Equals(opStr, "export.pdf", StringComparison.OrdinalIgnoreCase) || string.Equals(opStr, "export.excel", StringComparison.OrdinalIgnoreCase))
                {
                    var reqIdCaptura = reqIdStr;
                    var payloadCapturaJson = payloadJsonStr;
                    if (_uiControl != null && _uiControl.IsHandleCreated && !_uiControl.IsDisposed)
                    {
                        _uiControl.BeginInvoke(new Action(async () =>
                        {
                            await ExportarPdfAsyncConRespuesta(reqIdCaptura, payloadCapturaJson);
                        }));
                    }
                    else
                    {
                        _ = ExportarPdfAsyncConRespuesta(reqIdCaptura, payloadCapturaJson);
                    }
                    return;
                }

                JsonElement payloadElemFinal = default;
                if (payloadJsonStr != "null")
                {
                    using var pd3 = JsonDocument.Parse(payloadJsonStr);
                    payloadElemFinal = pd3.RootElement.Clone();
                }

                var datosFinal = Ejecutar(opStr, payloadElemFinal);
                Responder(requestId, true, datosFinal, null );
            }
            catch (DatosException ex)
            {
                Responder(requestId, false, null, ex.Message);
            }
            catch (Exception ex)
            {
                Responder(requestId, false, null, "Error interno: " + ex.Message);
            }
            finally
            {
                // envuelto ya no se usa
            }
        }

        private static object? Ejecutar(string op, JsonElement payload) => op switch
        {
            "ping" => new { pong = true, hora = DateTime.Now.ToString("HH:mm:ss") },

            "user.get" => new { nombre = RepoUsuario.ObtenerNombre() },
            "user.set" => GuardarNombre(payload),

            "projects.list" => ListarProyectos(),
            "projects.create" => CrearProyecto(payload),
            "projects.update" => EditarProyecto(payload),

            "entries.list" => ListarRegistros(payload),
            "entries.create" => CrearRegistro(payload),
            "entries.update" => EditarRegistro(payload),
            "entries.delete" => EliminarRegistro(payload),

            "export.pdf" => InstanciaActual.ExportarPdf(payload),
            "export.excel" => InstanciaActual.ExportarPdf(payload),

            _ => throw new DatosException($"Operación no permitida: {op}")
        };

        // ---------- Operaciones ----------

        private static object GuardarNombre(JsonElement p)
        {
            var nombre = TextoRequerido(p, "nombre");
            RepoUsuario.GuardarNombre(nombre);
            return new { nombre = nombre.Trim() };
        }

        private static object ListarProyectos()
        {
            return RepoProyectos.ListarConTotales()
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    creado = FechaDeCreacion(p.CreadoEn),
                    totalMinutos = p.TotalMinutos,
                    totalTexto = HorasYMinutos(p.TotalMinutos),
                    compania = p.Compania,
                    direccion = p.Direccion
                })
                .ToList();
        }

        private static object CrearProyecto(JsonElement p)
        {
            var nombre = TextoRequerido(p, "nombre");
            var compania = TextoOpcional(p, "compania") ?? "";
            var direccion = TextoOpcional(p, "direccion") ?? "";
            var id = RepoProyectos.Crear(nombre, compania, direccion);
            return new { id, nombre = nombre.Trim(), compania = compania.Trim(), direccion = direccion.Trim() };
        }

        private static object EditarProyecto(JsonElement p)
        {
            var id = EnteroRequerido(p, "id");
            var nombre = TextoRequerido(p, "nombre");
            var compania = TextoOpcional(p, "compania") ?? "";
            var direccion = TextoOpcional(p, "direccion") ?? "";
            RepoProyectos.Editar(id, nombre, compania, direccion);
            return new { id, nombre = nombre.Trim(), compania = compania.Trim(), direccion = direccion.Trim() };
        }

        private static object ListarRegistros(JsonElement p)
        {
            var idProyecto = EnteroRequerido(p, "idProyecto");
            return RepoRegistros.Listar(idProyecto)
                .Select(r => new
                {
                    id = r.Id,
                    idProyecto = r.IdProyecto,
                    fecha = Convertidores.IsoAFecha(r.Fecha),
                    inicio = Convertidores.MinutosAReloj(r.Inicio),
                    duracion = Convertidores.MinutosADuracion(r.Duracion),
                    nombre = r.Nombre,
                    numeroContacto = r.NumeroContacto,
                    totalMinutos = r.TotalMinutos,
                    totalTexto = Convertidores.MinutosADuracion(r.TotalMinutos) + " h",
                    observaciones = r.Observaciones
                })
                .ToList();
        }

        private static object CrearRegistro(JsonElement p)
        {
            var idProyecto = EnteroRequerido(p, "idProyecto");
            var fecha = Convertidores.FechaAIso(TextoRequerido(p, "fecha"));
            var inicio = Convertidores.RelojAMinutos(TextoRequerido(p, "inicio"));
            var duracion = Convertidores.DuracionAMinutos(TextoRequerido(p, "duracion"));
            var nombre = TextoRequerido(p, "nombre");
            var numeroContacto = TextoOpcional(p, "numeroContacto") ?? "";
            var observaciones = TextoOpcional(p, "observaciones");

            var id = RepoRegistros.Agregar(idProyecto, fecha, inicio, duracion, nombre, numeroContacto, observaciones);
            return new { id, totalMinutos = duracion };
        }

        private static object EditarRegistro(JsonElement p)
        {
            var id = EnteroRequerido(p, "id");
            var fecha = Convertidores.FechaAIso(TextoRequerido(p, "fecha"));
            var inicio = Convertidores.RelojAMinutos(TextoRequerido(p, "inicio"));
            var duracion = Convertidores.DuracionAMinutos(TextoRequerido(p, "duracion"));
            var nombre = TextoRequerido(p, "nombre");
            var numeroContacto = TextoOpcional(p, "numeroContacto") ?? "";
            var observaciones = TextoOpcional(p, "observaciones");

            RepoRegistros.Editar(id, fecha, inicio, duracion, nombre, numeroContacto, observaciones);
            return new { id, totalMinutos = duracion };
        }

        private static object EliminarRegistro(JsonElement p)
        {
            var id = EnteroRequerido(p, "id");
            RepoRegistros.Eliminar(id);
            return new { id };
        }

        private async Task ExportarPdfAsyncConRespuesta(string? requestId, string payloadJson)
        {
            JsonElement p = default;
            if (!string.IsNullOrWhiteSpace(payloadJson) && payloadJson != "null")
            {
                using var pd = JsonDocument.Parse(payloadJson);
                p = pd.RootElement.Clone();
            }
            try
            {
                var datosPdf = await ExportarPdfAsync(p);
                Responder(requestId, true, datosPdf, null );
            }
            catch (DatosException ex)
            {
                Responder(requestId, false, null, ex.Message);
            }
            catch (Exception ex)
            {
                Responder(requestId, false, null, ex.Message);
            }
        }

        private async Task ExportarPdfAsyncConRespuesta(string? requestId, JsonElement p)
        {
            try
            {
                var datosPdf = await ExportarPdfAsync(p);
                Responder(requestId, true, datosPdf, null );
            }
            catch (DatosException ex)
            {
                Responder(requestId, false, null, ex.Message);
            }
            catch (Exception ex)
            {
                Responder(requestId, false, null, ex.Message);
            }
        }

        private async Task<object> ExportarPdfAsync(JsonElement p)
        {
            var idProyecto = EnteroRequerido(p, "idProyecto");

            var proyectos = RepoProyectos.ListarConTotales();
            var proyecto = proyectos.FirstOrDefault(pr => pr.Id == idProyecto);
            var nombreProyecto = proyecto?.Nombre ?? $"Proyecto_{idProyecto}";

            string nombreLimpio = string.Concat(nombreProyecto.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch))).Trim();
            if (string.IsNullOrWhiteSpace(nombreLimpio)) nombreLimpio = $"Proyecto_{idProyecto}";
            string nombreSugerido = $"TimeSheet - {nombreLimpio} - {DateTime.Now:yyyy-MM-dd}.pdf";

            string? ruta = null;
            try
            {
                Form? formActivo = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                if (formActivo != null && formActivo.InvokeRequired)
                {
                    formActivo.Invoke(new Action(() =>
                    {
                        using var dlg = new SaveFileDialog
                        {
                            Filter = "Archivos PDF (*.pdf)|*.pdf",
                            FileName = nombreSugerido,
                            Title = "Guardar exportación PDF"
                        };
                        if (dlg.ShowDialog(formActivo) == DialogResult.OK)
                        {
                            ruta = dlg.FileName;
                        }
                    }));
                }
                else
                {
                    using var dlg = new SaveFileDialog
                    {
                        Filter = "Archivos PDF (*.pdf)|*.pdf",
                        FileName = nombreSugerido,
                        Title = "Guardar exportación PDF"
                    };
                    if (dlg.ShowDialog(formActivo) == DialogResult.OK)
                    {
                        ruta = dlg.FileName;
                    }
                }
            }
            catch
            {
                var escritorio = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                ruta = Path.Combine(escritorio, nombreSugerido);
            }

            if (string.IsNullOrWhiteSpace(ruta))
            {
                throw new DatosException("Exportación cancelada.");
            }

            try
            {
                string json = "{\"landscape\":true,\"printBackground\":true,\"preferCSSPageSize\":false,\"scale\":0.95,\"paperWidth\":11,\"paperHeight\":17,\"marginTop\":0.25,\"marginBottom\":0.25,\"marginLeft\":0.25,\"marginRight\":0.25}";
                string resultado = await _core.CallDevToolsProtocolMethodAsync("Page.printToPDF", json);
                using var docPdf = JsonDocument.Parse(resultado);
                if (docPdf.RootElement.TryGetProperty("data", out var dataEl))
                {
                    string b64 = dataEl.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(b64))
                    {
                        byte[] bytes = Convert.FromBase64String(b64);
                        File.WriteAllBytes(ruta, bytes);
                        return new { path = ruta };
                    }
                }
            }
            catch { }

            var settings = _core.Environment.CreatePrintSettings();
            settings.Orientation = CoreWebView2PrintOrientation.Landscape;
            settings.ShouldPrintBackgrounds = true;
            settings.ShouldPrintHeaderAndFooter = false;

            var stream = await _core.PrintToPdfStreamAsync(settings);

            using (var fs = File.Create(ruta))
            {
                await stream.CopyToAsync(fs);
            }
            stream.Dispose();

            return new { path = ruta };
        }

        private object ExportarPdf(JsonElement p)
        {
            return ExportarPdfAsync(p).GetAwaiter().GetResult();
        }

        // ---------- Respuesta ----------

        private void Responder(string? requestId, bool ok, object? data, string? error)
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["requestId"] = requestId,
                ["ok"] = ok,
                ["data"] = data,
                ["error"] = error
            });
            _core.PostWebMessageAsJson(json);
        }

        // ---------- Utilidades ----------

        private static bool EsOrigenPermitido(string? source)
        {
            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && string.Equals(uri.Host, HostPermitido, StringComparison.OrdinalIgnoreCase);
        }

        private static string? TextoOpcional(JsonElement p, string campo)
        {
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(campo, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static string TextoRequerido(JsonElement p, string campo)
        {
            var valor = TextoOpcional(p, campo);
            if (string.IsNullOrWhiteSpace(valor))
                throw new DatosException($"Falta el campo '{campo}'.");
            return valor;
        }

        private static long EnteroRequerido(JsonElement p, string campo)
        {
            if (p.ValueKind != JsonValueKind.Object ||
                !p.TryGetProperty(campo, out var v) ||
                !v.TryGetInt64(out var numero))
                throw new DatosException($"Falta el campo '{campo}'.");
            return numero;
        }

        /// <summary>"125h 30m" para las tarjetas del dashboard.</summary>
        private static string HorasYMinutos(long minutos)
        {
            if (minutos < 0) minutos = 0;
            return $"{minutos / 60}h {minutos % 60:00}m";
        }

        /// <summary>CreateAt viene como "YYYY-MM-DD HH:MM:SS"; la tarjeta muestra MM/dd/yyyy.</summary>
        private static string FechaDeCreacion(string createAt)
        {
            if (createAt.Length >= 10 &&
                DateTime.TryParseExact(createAt[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            return createAt;
        }
    }
}
