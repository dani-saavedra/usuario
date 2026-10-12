using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Usuarios.Api.Controllers;
using Usuarios.Api.Data;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Models.Requests;
using Usuarios.Api.Models.Responses;
using Usuarios.Api.Services;

namespace Usuarios.Api.Tests.Controllers
{
    [TestFixture]
    public class UsuariosControllerTests
    {
        private Mock<IUsuarioRepository> _repository;
        private Mock<IAuditService> _auditService;
        private UsuariosController _controller;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IUsuarioRepository>();
            _auditService = new Mock<IAuditService>();
            _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>())).Returns(Task.CompletedTask);

            _controller = new UsuariosController(_repository.Object, _auditService.Object);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/v1/usuarios/cerrar-sesion-bloquear");
            request.SetConfiguration(new HttpConfiguration());
            request.Properties[CorrelationKeys.RequestPropertyKey] = "correlation-test";
            request.Properties[CorrelationKeys.ConsumerPropertyKey] = "ConsumidorTest";

            _controller.Request = request;
            _controller.Configuration = request.GetConfiguration();
        }

        [Test]
        public async Task CerrarSesionYBloquear_UsuarioExistente_Retorna200YAuditaExito()
        {
            _repository.Setup(r => r.CerrarSesionYBloquearAsync("usuario.prueba1"))
                .ReturnsAsync(StoredProcedureResult.Of(0, "OK"));

            var response = await _controller.CerrarSesionYBloquear(new CerrarSesionBloquearRequest { Usuario = "usuario.prueba1" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await ReadBodyAsync<ApiResponse>(response);
            Assert.That(body.Codigo, Is.EqualTo("OK"));
            Assert.That(body.Operacion, Is.EqualTo("CERRAR_SESION_BLOQUEAR"));
            Assert.That(body.CorrelationId, Is.EqualTo("correlation-test"));

            _auditService.Verify(a => a.LogAsync(It.Is<AuditEntry>(e =>
                e.Resultado == "EXITO" && e.CodigoHttp == 200 && e.UsuarioObjetivo == "usuario.prueba1")), Times.Once);
        }

        [Test]
        public async Task CerrarSesionYBloquear_UsuarioInexistente_Retorna404()
        {
            _repository.Setup(r => r.CerrarSesionYBloquearAsync(It.IsAny<string>()))
                .ReturnsAsync(StoredProcedureResult.Of(1, "El usuario no existe"));

            var response = await _controller.CerrarSesionYBloquear(new CerrarSesionBloquearRequest { Usuario = "no-existe" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var body = await ReadBodyAsync<ErrorResponse>(response);
            Assert.That(body.Codigo, Is.EqualTo("USUARIO_NO_ENCONTRADO"));

            _auditService.Verify(a => a.LogAsync(It.Is<AuditEntry>(e =>
                e.Resultado == "FALLO" && e.CodigoHttp == 404)), Times.Once);
        }

        [Test]
        public async Task CerrarSesionYBloquear_ErrorDeEjecucionControlado_Retorna500()
        {
            _repository.Setup(r => r.CerrarSesionYBloquearAsync(It.IsAny<string>()))
                .ReturnsAsync(StoredProcedureResult.Of(2, "Violación de restricción"));

            var response = await _controller.CerrarSesionYBloquear(new CerrarSesionBloquearRequest { Usuario = "usuario.prueba1" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task CerrarSesionYBloquear_SqlServerNoDisponible_Retorna500YAuditaSinExponerDetalleInterno()
        {
            _repository.Setup(r => r.CerrarSesionYBloquearAsync(It.IsAny<string>()))
                .ThrowsAsync(new DataAccessException("fallo", new System.TimeoutException("Execution Timeout Expired")));

            var response = await _controller.CerrarSesionYBloquear(new CerrarSesionBloquearRequest { Usuario = "usuario.prueba1" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            var body = await ReadBodyAsync<ErrorResponse>(response);
            Assert.That(body.Mensaje, Does.Not.Contain("Timeout"));

            _auditService.Verify(a => a.LogAsync(It.Is<AuditEntry>(e =>
                e.ResultadoSql == "SQL_NO_DISPONIBLE")), Times.Once);
        }

        [Test]
        public async Task Desbloquear_UsuarioExistente_Retorna200()
        {
            _repository.Setup(r => r.DesbloquearAsync("usuario.prueba1"))
                .ReturnsAsync(StoredProcedureResult.Of(0, "OK"));

            var response = await _controller.Desbloquear(new DesbloquearRequest { Usuario = "usuario.prueba1" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await ReadBodyAsync<ApiResponse>(response);
            Assert.That(body.Operacion, Is.EqualTo("DESBLOQUEAR_USUARIO"));

            _repository.Verify(r => r.CerrarSesionYBloquearAsync(It.IsAny<string>()), Times.Never,
                "Desbloquear no debe invocar el procedimiento de cierre de sesión");
        }

        private static async Task<T> ReadBodyAsync<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
