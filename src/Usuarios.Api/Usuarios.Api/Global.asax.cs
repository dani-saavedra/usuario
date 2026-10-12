using System;
using System.Web.Http;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Falla rápido en el arranque si la configuración mínima no está presente.
            // Evita publicar una instancia que aceptaría tráfico sin poder validar
            // la API Key o sin cadena de conexión a SQL Server.
            StartupValidation.EnsureRequiredConfiguration();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception == null)
            {
                return;
            }

            // Red de seguridad para errores que ocurren fuera del pipeline de
            // Web API (por ejemplo, antes de que se resuelva una ruta). El
            // pipeline de Web API tiene su propio manejo centralizado en
            // Filters/GlobalExceptionHandler y Filters/GlobalExceptionLogger.
            FileAuditSink.LogUnhandledHostException(exception);
        }
    }
}
