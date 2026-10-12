using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api.Handlers
{
    /// <summary>
    /// Primer handler del pipeline: asegura que toda solicitud (incluidas las
    /// que luego sean rechazadas por API Key inválida) tenga un identificador
    /// de correlación, y lo refleja en la respuesta (Sección 7.1).
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = ResolveCorrelationId(request);

            request.Properties[CorrelationKeys.RequestPropertyKey] = correlationId;
            request.Properties[CorrelationKeys.StopwatchPropertyKey] = System.Diagnostics.Stopwatch.StartNew();

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.Headers.Contains(CorrelationKeys.HeaderName))
            {
                response.Headers.Add(CorrelationKeys.HeaderName, correlationId);
            }

            return response;
        }

        private static string ResolveCorrelationId(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(CorrelationKeys.HeaderName, out var values))
            {
                var provided = values.FirstOrDefault();

                // Se acepta un correlationId provisto por el consumidor (útil
                // para rastrear un flujo de punta a punta entre sistemas),
                // pero se valida su forma para evitar inyectar valores
                // arbitrarios en los registros de auditoría.
                if (!string.IsNullOrWhiteSpace(provided) && Guid.TryParse(provided, out var parsed))
                {
                    return parsed.ToString();
                }
            }

            return Guid.NewGuid().ToString();
        }
    }
}
