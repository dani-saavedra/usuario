using System.Net.Http.Formatting;
using System.Web.Http;
using Newtonsoft.Json.Serialization;
using Usuarios.Api.Filters;
using Usuarios.Api.Handlers;

namespace Usuarios.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Solo enrutamiento por atributos: los endpoints de negocio se
            // definen explícitamente en los controladores (Sección 4 del README).
            config.MapHttpAttributeRoutes();

            // El pipeline de handlers se ejecuta en el orden en que se agrega.
            // 1) Correlación: todo lo que ocurra después (incluyendo el 401 de
            //    API Key) queda asociado a un correlationId.
            // 2) API Key: corta la solicitud antes de llegar a cualquier
            //    controlador si la clave falta o es inválida.
            config.MessageHandlers.Add(new CorrelationIdHandler());
            config.MessageHandlers.Add(new ApiKeyAuthHandler());

            // Manejo centralizado de excepciones no controladas (Sección 8).
            config.Services.Replace(typeof(System.Web.Http.ExceptionHandling.IExceptionHandler), new GlobalExceptionHandler());
            config.Services.Add(typeof(System.Web.Http.ExceptionHandling.IExceptionLogger), new GlobalExceptionLogger());

            ConfigureJsonFormatter(config.Formatters.JsonFormatter);

            // La API solo expone JSON (Sección 4): se retira el formateador XML
            // para evitar negociación de contenido innecesaria.
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            config.Filters.Add(new ValidateModelStateAttribute());
        }

        private static void ConfigureJsonFormatter(JsonMediaTypeFormatter jsonFormatter)
        {
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            jsonFormatter.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;
        }
    }
}
