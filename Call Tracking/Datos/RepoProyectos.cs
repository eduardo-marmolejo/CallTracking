using Microsoft.Data.Sqlite;

namespace CallTracking.Datos
{
    public static class RepoProyectos
    {
        /// <summary>Todos los proyectos (más nuevos primero) con su total de minutos.</summary>
        public static List<Proyecto> ListarConTotales()
        {
            using var con = BaseDatos.Abrir();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT p.idProject,
                       p.Nombre,
                       COALESCE(SUM(r.Duracion), 0) AS TotalMinutos,
                       p.CreateAt,
                       p.Compania,
                       p.Direccion
                FROM Projects p
                LEFT JOIN RegistroLlamadas r ON r.idProject = p.idProject
                GROUP BY p.idProject, p.Nombre, p.CreateAt, p.Compania, p.Direccion
                ORDER BY p.CreateAt DESC, p.idProject DESC;";

            var lista = new List<Proyecto>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Proyecto(
                    Id: rd.GetInt64(0),
                    Nombre: rd.GetString(1),
                    TotalMinutos: rd.GetInt64(2),
                    CreadoEn: rd.GetString(3),
                    Compania: rd.GetString(4),
                    Direccion: rd.GetString(5)));
            return lista;
        }

        /// <summary>Crea un proyecto y devuelve su Id.</summary>
        public static long Crear(string? nombre, string? compania, string? direccion)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
                throw new DatosException("El nombre del proyecto no puede estar vacío.");

            compania = (compania ?? "").Trim();
            if (compania.Length == 0)
                throw new DatosException("El nombre de la compañía no puede estar vacío.");

            direccion = (direccion ?? "").Trim();

            try
            {
                using var con = BaseDatos.Abrir();
                using var cmd = con.CreateCommand();
                cmd.CommandText = "INSERT INTO Projects (Nombre, Compania, Direccion) VALUES (@n, @c, @d) RETURNING idProject;";
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@c", compania);
                cmd.Parameters.AddWithValue("@d", direccion);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19 &&
                                             ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                throw new DatosException("Ya existe un proyecto con ese nombre.");
            }
        }

        /// <summary>Actualiza los datos de un proyecto.</summary>
        public static void Editar(long id, string? nombre, string? compania, string? direccion)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
                throw new DatosException("El nombre del proyecto no puede estar vacío.");

            compania = (compania ?? "").Trim();
            if (compania.Length == 0)
                throw new DatosException("El nombre de la compañía no puede estar vacío.");

            direccion = (direccion ?? "").Trim();

            try
            {
                using var con = BaseDatos.Abrir();
                using var cmd = con.CreateCommand();
                cmd.CommandText = "UPDATE Projects SET Nombre = @n, Compania = @c, Direccion = @d WHERE idProject = @id;";
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@c", compania);
                cmd.Parameters.AddWithValue("@d", direccion);
                cmd.Parameters.AddWithValue("@id", id);
                int filas = cmd.ExecuteNonQuery();
                if (filas == 0)
                    throw new DatosException("El proyecto no existe.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19 &&
                                             ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                throw new DatosException("Ya existe un proyecto con ese nombre.");
            }
        }
    }
}
