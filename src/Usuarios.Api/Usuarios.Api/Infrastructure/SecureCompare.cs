using System.Text;

namespace Usuarios.Api.Infrastructure
{
    /// <summary>
    /// Comparación de cadenas en tiempo constante para evitar ataques de
    /// temporización (timing attacks) al validar la API Key (Sección 6.1).
    /// </summary>
    public static class SecureCompare
    {
        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            var bytesA = Encoding.UTF8.GetBytes(a);
            var bytesB = Encoding.UTF8.GetBytes(b);

            // .NET Framework 4.8 no expone CryptographicOperations.FixedTimeEquals,
            // por lo que se implementa manualmente: se recorre siempre la misma
            // cantidad de posiciones (la longitud del valor más largo) para que
            // el tiempo de ejecución no dependa de en qué posición difieren los
            // valores ni de un early-exit por longitud distinta.
            var length = bytesA.Length > bytesB.Length ? bytesA.Length : bytesB.Length;
            var diff = (uint)(bytesA.Length ^ bytesB.Length);

            for (var i = 0; i < length; i++)
            {
                var ca = i < bytesA.Length ? bytesA[i] : (byte)0;
                var cb = i < bytesB.Length ? bytesB[i] : (byte)0;
                diff |= (uint)(ca ^ cb);
            }

            return diff == 0;
        }
    }
}
