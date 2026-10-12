using System.Linq;
using System.Net;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Models.Responses;

namespace Usuarios.Api.Filters
{
    /// <summary>
    /// Devuelve 400 Bad Request de forma consistente cuando el modelo de
    /// entrada no pasa las validaciones de Data Annotations (Sección 8),
    /// sin exponer trazas internas (Sección 4.1).
    /// </summary>
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.ModelState.IsValid)
            {
                return;
            }

            var correlationId = actionContext.Request.GetCorrelationId();

            var errores = actionContext.ModelState
                .Where(kvp => kvp.Value.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                    ? $"{kvp.Key}: valor inválido"
                    : e.ErrorMessage))
                .ToList();

            var body = new ErrorResponse
            {
                Codigo = "SOLICITUD_INVALIDA",
                Mensaje = "La solicitud no cumple con el formato esperado",
                CorrelationId = correlationId,
                Errores = errores
            };

            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.BadRequest, body);
        }
    }
}
