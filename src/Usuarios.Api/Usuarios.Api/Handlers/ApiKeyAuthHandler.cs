using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Models.Responses;
using Usuarios.Api.Services;

namespace Usuarios.Api.Handlers
{
    /// <summary>
    /// Valida el encabezado X-API-Key antes de que la solicitud llegue a
    /// cualquier controlador (Sección 6.1). La clave nunca se acepta como
    /// parámetro de query string ni se incluye en los registros.
    /// </summary>
    public class ApiKeyAuthHandler : DelegatingHandler
    {
        private const string HeaderName = "X-API-Key";

        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IAuditService _auditService;

        public ApiKeyAuthHandler()
            : this(new ApiKeyProvider(), new SqlAuditService())
        {
        }

        public ApiKeyAuthHandler(IApiKeyProvider apiKeyProvider, IAuditService auditService)
        {
            _apiKeyProvider = apiKeyProvider;
            _auditService = auditService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = request.GetCorrelationId();

            var presentedKey = ExtractHeaderValue(request);

            if (string.IsNullOrEmpty(presentedKey) || !_apiKeyProvider.TryResolveConsumer(presentedKey, out var consumerName))
            {
                await _auditService.LogAsync(new AuditEntry
                {
                    CorrelationId = correlationId,
                    Operacion = request.RequestUri.AbsolutePath,
                    Consumidor = null,
                    UsuarioObjetivo = null,
                    Resultado = "FALLO",
                    CodigoHttp = (int)HttpStatusCode.Unauthorized,
                    ResultadoSql = null,
                    DetalleError = string.IsNullOrEmpty(presentedKey)
                        ? "Solicitud sin encabezado X-API-Key"
                        : "API Key inválida",
                    DuracionMs = request.GetElapsedMs()
                });

                return BuildUnauthorizedResponse(request, correlationId);
            }

            request.Properties[CorrelationKeys.ConsumerPropertyKey] = consumerName;

            return await base.SendAsync(request, cancellationToken);
        }

        private static string ExtractHeaderValue(HttpRequestMessage request)
        {
            // Deliberadamente NO se revisa request.RequestUri.Query: la API Key
            // nunca debe aceptarse como parámetro de URL (Sección 6.1), ya que
            // quedaría expuesta en logs de IIS, proxies y herramientas de red.
            if (request.Headers.TryGetValues(HeaderName, out var values))
            {
                return values.FirstOrDefault();
            }

            return null;
        }

        private static HttpResponseMessage BuildUnauthorizedResponse(HttpRequestMessage request, string correlationId)
        {
            var body = new ErrorResponse
            {
                Codigo = "NO_AUTORIZADO",
                Mensaje = "API Key ausente o inválida",
                CorrelationId = correlationId
            };

            var response = request.CreateResponse(HttpStatusCode.Unauthorized, body);
            return response;
        }
    }
}
