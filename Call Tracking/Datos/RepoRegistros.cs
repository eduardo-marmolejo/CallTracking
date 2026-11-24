using Microsoft.Data.Sqlite;
using System.Globalization;

namespace CallTracking.Datos
{
    public static class RepoRegistros
    {
        /// <summary>Registros de un proyecto, del más antiguo al más reciente.</summary>
        public static List<Registro> Listar(long idProyecto)
        {
            using var con = BaseDatos.Abrir();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT idRegistro, idProject, Fecha, horaInicio, Duracion, Nombre, NumeroContacto, Remarks
                FROM RegistroLlamadas
                WHERE idProject = @p
                ORDER BY Fecha ASC, horaInicio ASC, idRegistro ASC;";
            cmd.Parameters.AddWithValue("@p", idProyecto);

            var lista = new List<Registro>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Registro(rd.GetInt64(0), rd.GetInt64(1), rd.GetString(2),
                                       rd.GetInt32(3), rd.GetInt32(4), rd.GetString(5),
                                       rd.GetString(6), rd.GetString(7)));
            return lista;
        }

        /// <summary>Suma de minutos registrados en el proyecto.</summary>
        public static long TotalMinutos(long idProyecto)
        {
            using var con = BaseDatos.Abrir();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(Duracion), 0)
                FROM RegistroLlamadas WHERE idProject = @p;";
            cmd.Parameters.AddWithValue("@p", idProyecto);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        /// <summary>Agrega un registro y devuelve su Id. La fecha va en ISO (YYYY-MM-DD).</summary>
        public static long Agregar(long idProyecto, string fechaIso, int inicio, int duracion,
                                   string nombre, string? numeroContacto, string? observaciones)
        {
            Validar(fechaIso, inicio, duracion, nombre);
            try
            {
                using var con = BaseDatos.Abrir();
                using var cmd = con.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO RegistroLlamadas (idProject, Fecha, horaInicio, Duracion, Nombre, NumeroContacto, Remarks)
                    VALUES (@p, @f, @i, @dur, @nom, @contacto, @o)
                    RETURNING idRegistro;";
                Parametros(cmd, fechaIso, inicio, duracion, nombre, numeroContacto, observaciones);
                cmd.Parameters.AddWithValue("@p", idProyecto);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19 &&
                                             ex.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                throw new DatosException("El proyecto ya no existe.");
            }
        }

        /// <summary>Modifica un registro existente.</summary>
        public static void Editar(long idRegistro, string fechaIso, int inicio, int duracion,
                                  string nombre, string? numeroContacto, string? observaciones)
        {
            Validar(fechaIso, inicio, duracion, nombre);
            using var con = BaseDatos.Abrir();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                UPDATE RegistroLlamadas
                SET Fecha = @f, horaInicio = @i, Duracion = @dur, Nombre = @nom, NumeroContacto = @contacto, Remarks = @o,
                    UpdateAt = datetime('now', 'localtime')
                WHERE idRegistro = @id;";
            Parametros(cmd, fechaIso, inicio, duracion, nombre, numeroContacto, observaciones);
            cmd.Parameters.AddWithValue("@id", idRegistro);
            if (cmd.ExecuteNonQuery() == 0)
                throw new DatosException("El registro ya no existe.");
        }

        public static void Eliminar(long idRegistro)
        {
            using var con = BaseDatos.Abrir();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM RegistroLlamadas WHERE idRegistro = @id;";
            cmd.Parameters.AddWithValue("@id", idRegistro);
            cmd.ExecuteNonQuery();
        }

        private static void Parametros(SqliteCommand cmd, string fechaIso, int inicio, int duracion,
                                       string nombre, string? numeroContacto, string? obs)
        {
            cmd.Parameters.AddWithValue("@f", fechaIso);
            cmd.Parameters.AddWithValue("@i", inicio);
            cmd.Parameters.AddWithValue("@dur", duracion);
            cmd.Parameters.AddWithValue("@nom", nombre.Trim());
            cmd.Parameters.AddWithValue("@contacto", (numeroContacto ?? "").Trim());
            cmd.Parameters.AddWithValue("@o", (obs ?? "").Trim());
        }

        // Mensajes claros antes de llegar a los CHECK de la base (que son la red de seguridad).
        private static void Validar(string fechaIso, int inicio, int duracion, string nombre)
        {
            if (!DateTime.TryParseExact(fechaIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                throw new DatosException("La fecha no es válida.");
            if (inicio < 0 || inicio > 1439)
                throw new DatosException("La hora de inicio debe estar entre 00:00 y 23:59.");
            if (duracion < 0 || duracion > 1439)
                throw new DatosException("La duración debe estar entre 00:00 y 23:59.");
            if (string.IsNullOrWhiteSpace(nombre))
                throw new DatosException("El nombre no puede estar vacío.");
        }
    }
}
