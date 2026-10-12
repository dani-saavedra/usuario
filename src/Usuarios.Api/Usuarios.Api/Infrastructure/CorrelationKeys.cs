namespace Usuarios.Api.Infrastructure
{
    public static class CorrelationKeys
    {
        /// <summary>Encabezado HTTP de entrada/salida para el identificador de correlación.</summary>
        public const string HeaderName = "X-Correlation-Id";

        /// <summary>Clave usada en HttpRequestMessage.Properties para propagar el valor dentro del pipeline.</summary>
        public const string RequestPropertyKey = "Usuarios.Api.CorrelationId";

        /// <summary>Clave usada en HttpRequestMessage.Properties para propagar el nombre del consumidor autenticado.</summary>
        public const string ConsumerPropertyKey = "Usuarios.Api.Consumer";

        /// <summary>Clave usada en HttpRequestMessage.Properties para marcar el instante de inicio de la solicitud.</summary>
        public const string StopwatchPropertyKey = "Usuarios.Api.Stopwatch";
    }
}
