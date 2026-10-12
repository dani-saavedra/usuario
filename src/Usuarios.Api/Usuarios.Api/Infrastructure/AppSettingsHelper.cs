using System;
using System.Configuration;

namespace Usuarios.Api.Infrastructure
{
    /// <summary>
    /// Acceso tipado y con valores por defecto a appSettings, para evitar
    /// parsear configuración repetida en cada clase que la consume.
    /// </summary>
    public static class AppSettingsHelper
    {
        public static string GetString(string key, string defaultValue = null)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        public static string GetRequiredString(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(
                    $"La clave de configuración obligatoria '{key}' no está definida o está vacía.");
            }

            return value;
        }

        public static int GetInt(string key, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return int.TryParse(raw, out var value) ? value : defaultValue;
        }

        public static bool GetBool(string key, bool defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return bool.TryParse(raw, out var value) ? value : defaultValue;
        }
    }
}
