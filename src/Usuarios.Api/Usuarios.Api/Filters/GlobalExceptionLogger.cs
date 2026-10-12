using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Services;

namespace Usuarios.Api.Filters
{
    /// <summary>
    /// Punto único de registro para cualquier excepción no controlada que
    /// llegue hasta el pipeline de Web API (Sección 7.2: "Error inesperado de
    /// la API"). Separado de GlobalExceptionHandler porque Web API invoca
    /// todos los IExceptionLogger registrados aunque ya exista una respuesta,
    /// lo que garantiza que el registro ocurra incluso si algo falla al
    /// construir la respuesta de error.
    /// </summary>
    public class GlobalExceptionLogger : ExceptionLogger
    {
        private readonly IAuditService _auditService;

        public GlobalExceptionLogger()
            : this(new SqlAuditService())
        {
        }

        public GlobalExceptionLogger(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public override async Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var request = context.ExceptionContext?.Request;
            var correlationId = request?.GetCorrelationId() ?? "-";
            var operacion = request?.RequestUri?.AbsolutePath ?? "-";
            var exception = context.Exception;

            await _auditService.LogAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                Operacion = operacion,
                Resultado = "FALLO",
                CodigoHttp = 500,
                ResultadoSql = null,
                DetalleError = Truncate($"{exception.GetType().Name}: {exception.Message}", 2000)
            });

            // El detalle completo (incluyendo stack trace) solo se conserva en
            // el archivo de respaldo local, nunca en la respuesta al consumidor.
            FileAuditSink.LogAuditFallback(correlationId, operacion, exception.ToString(), exception);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }
    }
}
