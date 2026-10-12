using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Usuarios.Api.Data;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Models.Requests;
using Usuarios.Api.Models.Responses;
using Usuarios.Api.Services;

namespace Usuarios.Api.Controllers
{
    /// <summary>
    /// Implementa los dos endpoints descritos en la Sección 4 del README.
    /// La validación de API Key ya ocurrió en Handlers.ApiKeyAuthHandler antes
    /// de llegar aquí; este controlador asume un consumidor ya autenticado.
    /// </summary>
    [RoutePrefix("api/v1/usuarios")]
    public class UsuariosController : ApiController
    {
        private const string OperacionCerrarSesionBloquear = "CERRAR_SESION_BLOQUEAR";
        private const string OperacionDesbloquear = "DESBLOQUEAR_USUARIO";

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IAuditService _auditService;

        public UsuariosController()
            : this(new SqlUsuarioRepository(), new SqlAuditService())
        {
        }

        public UsuariosController(IUsuarioRepository usuarioRepository, IAuditService auditService)
        {
            _usuarioRepository = usuarioRepository;
            _auditService = auditService;
        }

        [HttpPost]
        [Route("cerrar-sesion-bloquear")]
        public async Task<HttpResponseMessage> CerrarSesionYBloquear([FromBody] CerrarSesionBloquearRequest request)
        {
            return await EjecutarOperacionAsync(
                operacion: OperacionCerrarSesionBloquear,
                usuario: request.Usuario,
                mensajeExito: "Sesiones cerradas y usuario bloqueado correctamente",
                ejecutar: () => _usuarioRepository.CerrarSesionYBloquearAsync(request.Usuario));
        }

        [HttpPost]
        [Route("desbloquear")]
        public async Task<HttpResponseMessage> Desbloquear([FromBody] DesbloquearRequest request)
        {
            return await EjecutarOperacionAsync(
                operacion: OperacionDesbloquear,
                usuario: request.Usuario,
                mensajeExito: "Usuario desbloqueado correctamente",
                ejecutar: () => _usuarioRepository.DesbloquearAsync(request.Usuario));
        }

        private async Task<HttpResponseMessage> EjecutarOperacionAsync(
            string operacion,
            string usuario,
            string mensajeExito,
            Func<Task<StoredProcedureResult>> ejecutar)
        {
            var correlationId = Request.GetCorrelationId();
            var consumidor = Request.GetConsumer();

            try
            {
                var resultado = await ejecutar();

                switch (resultado.ResultCode)
                {
                    case ResultCode.Ok:
                        await AuditarAsync(operacion, consumidor, usuario, correlationId, "EXITO", (int)HttpStatusCode.OK, resultado);
                        return CrearRespuestaExitosa(operacion, mensajeExito, correlationId);

                    case ResultCode.UsuarioNoExiste:
                        await AuditarAsync(operacion, consumidor, usuario, correlationId, "FALLO", (int)HttpStatusCode.NotFound, resultado);
                        return CrearRespuestaError(
                            HttpStatusCode.NotFound, "USUARIO_NO_ENCONTRADO", "El usuario indicado no existe", correlationId);

                    default:
                        await AuditarAsync(operacion, consumidor, usuario, correlationId, "FALLO", (int)HttpStatusCode.InternalServerError, resultado);
                        return CrearRespuestaError(
                            HttpStatusCode.InternalServerError, "ERROR_INTERNO", "Ocurrió un error al procesar la operación", correlationId);
                }
            }
            catch (DataAccessException ex)
            {
                // SQL Server no disponible, timeout, o fallo de infraestructura
                // de datos (Sección 10). Se registra el tipo de error sin
                // exponer el mensaje original de ADO.NET al consumidor.
                await _auditService.LogAsync(new AuditEntry
                {
                    CorrelationId = correlationId,
                    Operacion = operacion,
                    Consumidor = consumidor,
                    UsuarioObjetivo = usuario,
                    Resultado = "FALLO",
                    CodigoHttp = (int)HttpStatusCode.InternalServerError,
                    ResultadoSql = "SQL_NO_DISPONIBLE",
                    DetalleError = ex.InnerException?.Message,
                    DuracionMs = Request.GetElapsedMs()
                });

                return CrearRespuestaError(
                    HttpStatusCode.InternalServerError, "ERROR_INTERNO",
                    "El servicio no pudo completar la operación en este momento", correlationId);
            }
        }

        private Task AuditarAsync(
            string operacion, string consumidor, string usuario, string correlationId,
            string resultado, int codigoHttp, StoredProcedureResult sp)
        {
            return _auditService.LogAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                Operacion = operacion,
                Consumidor = consumidor,
                UsuarioObjetivo = usuario,
                Resultado = resultado,
                CodigoHttp = codigoHttp,
                ResultadoSql = sp.ResultCode.ToString(),
                DetalleError = resultado == "EXITO" ? null : sp.ResultMessage,
                DuracionMs = Request.GetElapsedMs()
            });
        }

        private HttpResponseMessage CrearRespuestaExitosa(string operacion, string mensaje, string correlationId)
        {
            var body = new ApiResponse
            {
                Codigo = "OK",
                Mensaje = mensaje,
                Operacion = operacion,
                CorrelationId = correlationId
            };

            return Request.CreateResponse(HttpStatusCode.OK, body);
        }

        private HttpResponseMessage CrearRespuestaError(HttpStatusCode statusCode, string codigo, string mensaje, string correlationId)
        {
            var body = new ErrorResponse
            {
                Codigo = codigo,
                Mensaje = mensaje,
                CorrelationId = correlationId
            };

            return Request.CreateResponse(statusCode, body);
        }
    }
}
