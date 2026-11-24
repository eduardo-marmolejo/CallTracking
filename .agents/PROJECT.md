# Call Tracking

Aplicación de escritorio para Windows para registrar y consultar llamadas/tiempos por proyecto. La interfaz es web (HTML/CSS/JS) alojada dentro de una app WinForms mediante WebView2; los datos se guardan en SQLite local.

## Resumen técnico

- **Plataforma:** .NET 10, WinForms, WebView2.
- **Paquetes NuGet:** `Microsoft.Data.Sqlite` 10.0.14, `Microsoft.Web.WebView2` 1.0.4314.35.
- **Base de datos:** SQLite (`CallTracking.db`) junto al ejecutable. Esquema embebido en `Datos\Esquema.sql` (`PRAGMA user_version = 1`).
- **Frontend:** archivos estáticos en `web/`, servidos por WebView2 con host virtual `https://app.local/`.
- **Idioma UI:** español e inglés (selector ES/EN). Tema claro/oscuro.

## Estructura principal

```text
CallTracking.slnx
Call Tracking/
  CallTracking.csproj
  Program.cs                  # Arranque, backup de BD, inicialización
  Form1.cs                    # Ventana WinForms + WebView2 + puente
  Form1.Designer.cs
  Datos/
    Esquema.sql               # Esquema SQLite v1 (recurso embebido)
    BaseDatos.cs              # Ruta/conexión/inicialización de BD
    Modelos.cs                # Proyecto, Registro, DatosException
    Convertidores.cs          # Conversiones fecha/hora/duración
    RepoUsuario.cs            # Nombre de usuario
    RepoProyectos.cs          # CRUD de proyectos + totales
    RepoRegistros.cs          # CRUD de registros + totales
  WebView/
    JsBridge.cs               # Puente C# <-> JS (lista blanca)
  web/
    index.html                # Dashboard
    registroLlamadas.html     # Detalle de proyecto / registros
    css/styles.css
    js/bridge.js              # API JS window.tt
    js/script.js              # Lógica UI, i18n, render, eventos
    img/                      # Logos
    fonts/                    # Inter / JetBrains Mono
```

## Arranque y persistencia

1. `Program.Main` inicializa WinForms.
2. Intenta crear copia de seguridad de `CallTracking.db` en `backups/` con nombre `CallTracking_yyyy-MM-dd_HH-mm-ss.db`; conserva las 6 más recientes. Si falla, no bloquea el arranque.
3. `BaseDatos.Inicializar()` abre/crea la BD con `ForeignKeys=true`.
   - BD nueva: aplica `Esquema.sql` en una transacción.
   - BD sin versión pero con tablas: lanza error.
   - BD con versión mayor a la soportada: lanza error.
   - `VersionActual = 1`.
4. Abre `frmCallTracking`, que carga WebView2, mapea `app.local` a la carpeta `web`, crea `JsBridge` y navega a `https://app.local/index.html`.

## Base de datos (esquema v1)

Todas las tablas son `STRICT`.

### Usuarios
- `idUsuarios` INTEGER PK AUTOINCREMENT.
- `Nombre` TEXT NOT NULL COLLATE NOCASE, sin vacío.
- `CreateAt` TEXT DEFAULT `datetime('now','localtime')`.

Uso: guardar el nombre del usuario para el saludo del dashboard.

### Projects
- `idProject` INTEGER PK AUTOINCREMENT.
- `Nombre` TEXT NOT NULL COLLATE NOCASE, sin vacío. Índice único `UX_Projects_Nombre`.
- `Compania` TEXT NOT NULL DEFAULT `''`.
- `Direccion` TEXT NOT NULL DEFAULT `''`.
- `CreateAt` TEXT DEFAULT localtime.

### RegistroLlamadas
- `idRegistro` INTEGER PK AUTOINCREMENT.
- `idProject` INTEGER NOT NULL FK a `Projects(idProject)` ON DELETE RESTRICT.
- `Fecha` TEXT NOT NULL, ISO `YYYY-MM-DD` con CHECK de formato y `date(Fecha)=Fecha`.
- `horaInicio` INTEGER NOT NULL, minutos desde medianoche, `0..1439`.
- `Duracion` INTEGER NOT NULL DEFAULT 0, minutos, `0..1439`.
- `Nombre` TEXT NOT NULL COLLATE NOCASE, sin vacío.
- `NumeroContacto` TEXT NOT NULL DEFAULT `''`.
- `Remarks` TEXT NOT NULL DEFAULT `''`.
- `CreateAt` / `UpdateAt` TEXT DEFAULT localtime.

Índice: `IX_RegistroLlamadas_Project_Fecha (idProject, Fecha DESC)`.

## Modelos y conversiones

- `Proyecto(Id, Nombre, TotalMinutos, CreadoEn, Compania, Direccion)`.
- `Registro(Id, IdProyecto, Fecha, Inicio, Duracion, Nombre, NumeroContacto, Observaciones)`; `TotalMinutos => Duracion`.
- `DatosException`: error de validación/datos con mensaje para UI.

`Convertidores`:
- Fecha UI `MM/dd/yyyy` ↔ BD `YYYY-MM-DD`.
- Inicio/reloj UI `hh:mm AM/PM` ↔ minutos `0..1439` (acepta 24h como tolerancia de entrada).
- Duración UI `HH:MM` ↔ minutos.

## Repositorios

### RepoUsuario
- `ObtenerNombre()`: primer nombre o `null`.
- `GuardarNombre(nombre)`: inserta o actualiza el nombre existente.

### RepoProyectos
- `ListarConTotales()`: proyectos con `COALESCE(SUM(r.Duracion),0)`, ordenados por creación descendente.
- `Crear(nombre, compania, direccion)`: valida nombre y compañía requeridos; dirección opcional; nombre único.
- `Editar(id, nombre, compania, direccion)`: actualiza proyecto; valida existencia y unicidad.

### RepoRegistros
- `Listar(idProyecto)`: registros ordenados por fecha, hora inicio e id ascendente.
- `TotalMinutos(idProyecto)`: `SUM(Duracion)`.
- `Agregar(...)`: valida e inserta; FK inexistente => error claro.
- `Editar(...)`: valida y actualiza; pone `UpdateAt`.
- `Eliminar(idRegistro)`.

Validaciones: fecha ISO válida, inicio/duración en rango, nombre no vacío. Los CHECK de BD actúan como red de seguridad.

## Puente WebView2 (C# ↔ JS)

`JsBridge` recibe mensajes WebView2 solo desde `https://app.local`. Contrato:

- Solicitud: `{ op, requestId, payload }`
- Respuesta: `{ requestId, ok, data | error }`

Operaciones permitidas:

| op | Payload | Resultado |
|---|---|---|
| `ping` | - | `{ pong, hora }` |
| `user.get` | - | `{ nombre }` |
| `user.set` | `{ nombre }` | `{ nombre }` |
| `projects.list` | - | lista de proyectos con totales |
| `projects.create` | `{ nombre, compania, direccion }` | proyecto creado |
| `projects.update` | `{ id, nombre, compania, direccion }` | proyecto actualizado |
| `entries.list` | `{ idProyecto }` | registros del proyecto |
| `entries.create` | `{ idProyecto, fecha, inicio, duracion, nombre, numeroContacto, observaciones }` | `{ id, totalMinutos }` |
| `entries.update` | `{ id, fecha, inicio, duracion, nombre, numeroContacto, observaciones }` | `{ id, totalMinutos }` |
| `entries.delete` | `{ id }` | `{ id }` |
| `export.pdf` / `export.excel` | `{ idProyecto }` | `{ path }` o cancelación |

Notas:
- `entries.list` devuelve `fecha` en MM/dd/yyyy, `inicio` en reloj 12h, `duracion` como HH:MM, `totalTexto` como `HH:MM h`, además de campos del registro.
- `export.pdf` abre `SaveFileDialog`, genera PDF con `Page.printToPDF` (DevTools) y tiene fallback a `PrintToPdfStreamAsync`.

## API JS (`web/js/bridge.js`)

Expone `window.tt`:

- `tt.disponible()`, `tt.llamar(op, payload)`, `tt.ping()`
- `tt.usuario.obtener()`, `tt.usuario.guardar(nombre)`
- `tt.proyectos.listar()`, `tt.proyectos.crear(nombre, compania, direccion)`, `tt.proyectos.editar(id, nombre, compania, direccion)`
- `tt.registros.listar(idProyecto)`, `tt.registros.crear(datos)`, `tt.registros.editar(datos)`, `tt.registros.eliminar(id)`
- `tt.exportarPdf(idProyecto)`

Usa `requestId`, promesas pendientes y timeout de 60 s.

## Interfaz web

### Dashboard (`index.html`)
- Saludo según hora y nombre de usuario.
- Tema claro/oscuro persistido en `localStorage`.
- Selector de idioma ES/EN.
- Configuración para cambiar nombre de usuario.
- Crear y editar proyectos (nombre, compañía, dirección).
- Tarjetas de proyecto con fecha de creación y duración total de llamada.
- Filtro de proyectos por nombre o compañía.

### Detalle de proyecto (`registroLlamadas.html`)
- Encabezado con breadcrumb, nombre, compañía, dirección y tarjeta **Duración Total de Llamada / Total Call Duration**.
- Exportar a PDF.
- Tabla de registros con 8 columnas: selección, Fecha, Inicio, Duración, Nombre, Número de Contacto, Observaciones, Acciones.
- Fila de captura: inputs de una sola línea; Observaciones usa input de texto.
- Búsqueda por observaciones, fecha o nombre.
- Selección por filas y seleccionar todo; el total mostrado refleja la selección cuando aplica.
- Edición inline: al hacer clic en una fila se habilitan inputs; Observaciones se edita mediante botón compacto que abre un modal con textarea amplio (Aceptar/Cancelar/Escape/clic fuera). La fila no cambia de ancho al editar.
- Eliminar registro con confirmación.
- Fórmula mostrada: **Duración Total = suma de Duraciones / Total Duration = sum of Durations**.

## Reglas de negocio vigentes

- `Inicio` se captura/valida como hora de reloj; no se usa para calcular totales.
- `Duracion` (HH:MM, 0..1439) es el único campo sumado en totales.
- `Nombre` del registro es obligatorio.
- `NumeroContacto` y `Remarks` son opcionales (cadena vacía por defecto).
- Los totales de proyecto se calculan con `SUM(Duracion)`.

## Compilación

Desde la raíz del workspace:

```powershell
dotnet build "Call Tracking\CallTracking.csproj" -c Debug
dotnet build "Call Tracking\CallTracking.csproj" -c Release
```

La carpeta `web\**\*` se copia al output (`PreserveNewest`). `Datos\Esquema.sql` se embebe como recurso `Esquema.sql`.

## Notas

- La app crea la BD automáticamente si no existe; no hay migraciones incrementales más allá de la inicialización v1.
- `Form1.resx` y partes del proyecto conservan nombres/artefactos históricos (por ejemplo `frmCallTracking`), pero la identidad actual del producto es **Call Tracking**.
- Los archivos en `bin/`, `obj/`, `publish/` y `backups/` son artefactos generados/runtime; no son fuente principal.
