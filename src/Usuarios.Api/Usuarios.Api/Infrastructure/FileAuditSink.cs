using System;
using System.IO;
using System.Text;

namespace Usuarios.Api.Infrastructure
{
    /// <summary>
    /// Respaldo de auditoría en archivo plano. Se usa únicamente cuando la
    /// escritura en la tabla de auditoría de SQL Server falla, para que un
    /// error de registro no provoque que la operación de negocio quede sin
    /// ningún rastro (Sección 7.3) y, por separado, para errores del host que
    /// ocurren fuera del pipeline de Web API.
    ///
    /// No debe usarse como mecanismo primario de auditoría: un archivo local
    /// en un servidor IIS no ofrece las garantías de consulta, retención ni
    /// acceso concurrente que exige la Sección 7. Revisar periódicamente esta
    /// carpeta es responsabilidad operativa hasta que exista un mecanismo
    /// centralizado (Sección 7.3).
    /// </summary>
    public static class FileAuditSink
    {
        private static readonly object SyncRoot = new object();

        private static string LogDirectory => AppSettingsHelper.GetString(
            "Auditoria.DirectorioRespaldo",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs"));

        public static void LogAuditFallback(string correlationId, string operacion, string detalle, Exception ex)
        {
            WriteLine("audit-fallback", correlationId, operacion, detalle, ex);
        }

        public static void LogUnhandledHostException(Exception ex)
        {
            WriteLine("host-error", correlationId: null, operacion: "HOST", detalle: "Excepción no controlada fuera del pipeline de Web API", ex: ex);
        }

        private static void WriteLine(string categoria, string correlationId, string operacion, string detalle, Exception ex)
        {
            try
            {
                var directory = LogDirectory;

                lock (SyncRoot)
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var fileName = $"{categoria}-{DateTime.UtcNow:yyyy-MM-dd}.log";
                    var path = Path.Combine(directory, fileName);

                    var line = new StringBuilder();
                    line.Append(DateTime.UtcNow.ToString("o"));
                    line.Append('\t').Append(categoria);
                    line.Append('\t').Append(correlationId ?? "-");
                    line.Append('\t').Append(operacion ?? "-");
                    line.Append('\t').Append(Sanitize(detalle));
                    if (ex != null)
                    {
                        line.Append('\t').Append(Sanitize(ex.GetType().Name + ": " + ex.Message));
                    }

                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Último nivel de la cadena de auditoría: si ni siquiera se
                // puede escribir en disco, no se debe tumbar la solicitud en
                // curso ni generar una excepción secundaria que oculte la
                // original.
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "-";
            }

            return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
