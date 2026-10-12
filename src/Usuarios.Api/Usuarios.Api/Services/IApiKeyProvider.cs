using System.Collections.Generic;

namespace Usuarios.Api.Services
{
    public interface IApiKeyProvider
    {
        /// <summary>
        /// Intenta resolver la API Key presentada por el consumidor a un
        /// nombre lógico de consumidor (Sección 6.1: "claves independientes
        /// por sistema consumidor").
        /// </summary>
        bool TryResolveConsumer(string presentedApiKey, out string consumerName);

        bool HasAnyKeyConfigured();

        IEnumerable<KeyValuePair<string, string>> GetConfiguredKeys();
    }
}
