namespace CallTracking.Datos
{
    /// <summary>Error de validación o de datos con un mensaje listo para mostrar al usuario.</summary>
    public class DatosException : Exception
    {
        public DatosException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Proyecto con el total de minutos registrados.</summary>
    public record Proyecto(long Id, string Nombre, long TotalMinutos, string CreadoEn, string Compania, string Direccion);

    /// <summary>Un registro de tiempo. Fecha ISO (YYYY-MM-DD), inicio en minutos desde medianoche y duración en minutos.</summary>
    public record Registro(long Id, long IdProyecto, string Fecha, int Inicio, int Duracion, string Nombre, string NumeroContacto, string Observaciones)
    {
        public int TotalMinutos => Duracion;
    }
}
