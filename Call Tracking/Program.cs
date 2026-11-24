using CallTracking.Datos;

namespace CallTracking
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            try
            {
                RealizarCopiaSeguridad();
            }
            catch { }

            try
            {
                BaseDatos.Inicializar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo preparar la base de datos." + Environment.NewLine +
                    Environment.NewLine +
                    BaseDatos.RutaArchivo + Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "Call Tracking",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new frmCallTracking());
        }

        private static void RealizarCopiaSeguridad()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string dbPath = Path.Combine(baseDir, "CallTracking.db");

                if (!File.Exists(dbPath))
                    return;

                string backupDir = Path.Combine(baseDir, "backups");
                Directory.CreateDirectory(backupDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupPath = Path.Combine(backupDir, $"CallTracking_{timestamp}.db");

                File.Copy(dbPath, backupPath, overwrite: false);

                // Mantener solo las 6 últimas copias
                try
                {
                    var archivos = new DirectoryInfo(backupDir)
                        .GetFiles("CallTracking_*.db")
                        .OrderByDescending(f => f.Name)
                        .ToList();

                    if (archivos.Count > 6)
                    {
                        for (int i = 6; i < archivos.Count; i++)
                        {
                            try { archivos[i].Delete(); } catch { }
                        }
                    }
                }
                catch { }
            }
            catch
            {
                // No bloquear el arranque si falla la copia de seguridad
            }
        }
    }
}