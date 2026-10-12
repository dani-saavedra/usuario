using System;

namespace Usuarios.Api.Data
{
    /// <summary>
    /// Envuelve cualquier excepción de ADO.NET (timeout, SQL Server no
    /// disponible, etc.) para que el controlador pueda distinguir este caso
    /// (Sección 10: "SQL Server no disponible") del resto de errores
    /// inesperados, sin exponer el mensaje original de SqlException al
    /// consumidor.
    /// </summary>
    public class DataAccessException : Exception
    {
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
