using System;
using System.Configuration;
using System.Linq;
using Usuarios.Api.Services;

namespace Usuarios.Api.Infrastructure
{
    /// <summary>
    /// Validaciones de arranque: es preferible que la aplicación no inicie a
    /// que inicie y acepte tráfico sin poder validar la API Key o sin acceso
    /// configurado a SQL Server (Criterio de finalización, Sección 12).
    /// </summary>
    public static class StartupValidation
    {
        public static void EnsureRequiredConfiguration()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["UsuariosDb"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la cadena de conexión 'UsuariosDb'. Revisar Web.config / protected configuration.");
            }

            var apiKeyProvider = new ApiKeyProvider();
            if (!apiKeyProvider.HasAnyKeyConfigured())
            {
                throw new ConfigurationErrorsException(
                    "No hay ninguna API Key configurada en Secrets\\ApiKeys.config. " +
                    "La API no debe aceptar tráfico sin al menos un consumidor autorizado.");
            }

            var duplicated = apiKeyProvider.GetConfiguredKeys()
                .GroupBy(k => k.Value, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicated.Any())
            {
                throw new ConfigurationErrorsException(
                    "Hay API Keys duplicadas entre distintos consumidores en Secrets\\ApiKeys.config. " +
                    "Cada consumidor debe tener una clave independiente (Sección 6.1).");
            }
        }
    }
}
