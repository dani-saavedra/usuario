using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api.Services
{
    /// <summary>
    /// Lee las API Keys configuradas por consumidor desde appSettings.
    /// Las claves reales se mantienen en el archivo externo
    /// Secrets\ApiKeys.config (referenciado desde Web.config mediante
    /// &lt;appSettings file="Secrets\ApiKeys.config"&gt;), fuera del control de
    /// versiones y protegible de forma independiente con aspnet_regiis
    /// (Sección 6.1: "mantener las claves fuera del código fuente y del
    /// repositorio").
    ///
    /// Convención de claves: ApiKey:&lt;NombreConsumidor&gt; = &lt;valor-secreto&gt;
    /// </summary>
    public class ApiKeyProvider : IApiKeyProvider
    {
        private const string KeyPrefix = "ApiKey:";

        public bool TryResolveConsumer(string presentedApiKey, out string consumerName)
        {
            consumerName = null;

            if (string.IsNullOrEmpty(presentedApiKey))
            {
                return false;
            }

            foreach (var entry in GetConfiguredKeys())
            {
                if (SecureCompare.FixedTimeEquals(entry.Value, presentedApiKey))
                {
                    consumerName = entry.Key;
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyKeyConfigured()
        {
            return GetConfiguredKeys().Any();
        }

        public IEnumerable<KeyValuePair<string, string>> GetConfiguredKeys()
        {
            var appSettings = ConfigurationManager.AppSettings;

            foreach (var key in appSettings.AllKeys)
            {
                if (key != null && key.StartsWith(KeyPrefix, System.StringComparison.Ordinal))
                {
                    var consumer = key.Substring(KeyPrefix.Length);
                    var value = appSettings[key];

                    if (!string.IsNullOrWhiteSpace(consumer) && !string.IsNullOrWhiteSpace(value))
                    {
                        yield return new KeyValuePair<string, string>(consumer, value);
                    }
                }
            }
        }
    }
}
