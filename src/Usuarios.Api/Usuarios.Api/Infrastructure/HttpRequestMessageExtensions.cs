using System.Diagnostics;
using System.Net.Http;

namespace Usuarios.Api.Infrastructure
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetCorrelationId(this HttpRequestMessage request)
        {
            return request.Properties.TryGetValue(CorrelationKeys.RequestPropertyKey, out var value)
                ? (string)value
                : "-";
        }

        public static string GetConsumer(this HttpRequestMessage request)
        {
            return request.Properties.TryGetValue(CorrelationKeys.ConsumerPropertyKey, out var value)
                ? (string)value
                : null;
        }

        public static long GetElapsedMs(this HttpRequestMessage request)
        {
            if (request.Properties.TryGetValue(CorrelationKeys.StopwatchPropertyKey, out var value)
                && value is Stopwatch stopwatch)
            {
                return stopwatch.ElapsedMilliseconds;
            }

            return 0;
        }
    }
}
