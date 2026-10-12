using System.Net;
using System.Net.Http;
using System.Web.Http.ExceptionHandling;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Models.Responses;

namespace Usuarios.Api.Filters
{
    /// <summary>
    /// Traduce cualquier excepción no controlada en una respuesta 500
    /// consistente y sin trazas internas (Sección 4.1, Sección 8). El
    /// registro de la excepción lo hace GlobalExceptionLogger por separado.
    /// </summary>
    public class GlobalExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            var request = context.ExceptionContext.Request;
            var correlationId = request.GetCorrelationId();

            var body = new ErrorResponse
            {
                Codigo = "ERROR_INTERNO",
                Mensaje = "Ocurrió un error inesperado al procesar la solicitud",
                CorrelationId = correlationId
            };

            context.Result = new ResponseMessageResult(
                request.CreateResponse(HttpStatusCode.InternalServerError, body));
        }
    }

    internal class ResponseMessageResult : System.Web.Http.IHttpActionResult
    {
        private readonly HttpResponseMessage _response;

        public ResponseMessageResult(HttpResponseMessage response)
        {
            _response = response;
        }

        public System.Threading.Tasks.Task<HttpResponseMessage> ExecuteAsync(System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(_response);
        }
    }
}
