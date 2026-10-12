using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Moq;
using NUnit.Framework;
using Usuarios.Api.Handlers;
using Usuarios.Api.Infrastructure;
using Usuarios.Api.Services;

namespace Usuarios.Api.Tests.Handlers
{
    [TestFixture]
    public class ApiKeyAuthHandlerTests
    {
        private Mock<IApiKeyProvider> _apiKeyProvider;
        private Mock<IAuditService> _auditService;

        [SetUp]
        public void SetUp()
        {
            _apiKeyProvider = new Mock<IApiKeyProvider>();
            _auditService = new Mock<IAuditService>();
            _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>())).Returns(Task.CompletedTask);
        }

        [Test]
        public async Task SendAsync_SinEncabezadoApiKey_Retorna401YNoLlamaAlInnerHandler()
        {
            var handler = CreateHandler(innerCalled: () => Assert.Fail("No debía invocarse el siguiente handler"));
            var request = CreateRequest(apiKey: null);

            var response = await new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            _auditService.Verify(a => a.LogAsync(It.Is<AuditEntry>(e =>
                e.Resultado == "FALLO" && e.CodigoHttp == 401)), Times.Once);
        }

        [Test]
        public async Task SendAsync_ApiKeyInvalida_Retorna401()
        {
            _apiKeyProvider
                .Setup(p => p.TryResolveConsumer(It.IsAny<string>(), out It.Ref<string>.IsAny))
                .Returns(false);

            var handler = CreateHandler(innerCalled: () => Assert.Fail("No debía invocarse el siguiente handler"));
            var request = CreateRequest(apiKey: "clave-invalida");

            var response = await new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task SendAsync_ApiKeyValida_ContinuaElPipelineYPropagaConsumidor()
        {
            var resolvedConsumer = "ConsumidorA";
            _apiKeyProvider
                .Setup(p => p.TryResolveConsumer("clave-valida", out resolvedConsumer))
                .Returns(true);

            string consumerSeenByInnerHandler = null;
            var handler = CreateHandler(innerCalled: () => { });
            var inner = (RecordingInnerHandler)handler.InnerHandler;
            inner.OnRequest = req => consumerSeenByInnerHandler = req.GetConsumer();

            var request = CreateRequest(apiKey: "clave-valida");

            var response = await new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(consumerSeenByInnerHandler, Is.EqualTo("ConsumidorA"));
            _auditService.Verify(a => a.LogAsync(It.IsAny<AuditEntry>()), Times.Never);
        }

        private ApiKeyAuthHandler CreateHandler(System.Action innerCalled)
        {
            var handler = new ApiKeyAuthHandler(_apiKeyProvider.Object, _auditService.Object)
            {
                InnerHandler = new RecordingInnerHandler(innerCalled)
            };

            return handler;
        }

        private static HttpRequestMessage CreateRequest(string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/v1/usuarios/cerrar-sesion-bloquear");
            request.SetConfiguration(new HttpConfiguration());

            if (apiKey != null)
            {
                request.Headers.Add("X-API-Key", apiKey);
            }

            return request;
        }

        private class RecordingInnerHandler : DelegatingHandler
        {
            private readonly System.Action _onSend;

            public System.Action<HttpRequestMessage> OnRequest { get; set; }

            public RecordingInnerHandler(System.Action onSend)
            {
                _onSend = onSend;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _onSend();
                OnRequest?.Invoke(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
