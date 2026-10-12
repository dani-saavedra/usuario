using System.Collections.Generic;

namespace Usuarios.Api.Models.Responses
{
    /// <summary>
    /// Forma de respuesta de error. Nunca debe incluir detalles internos de
    /// SQL Server ni información sensible del usuario (Sección 4.1).
    /// </summary>
    public class ErrorResponse
    {
        public string Codigo { get; set; }
        public string Mensaje { get; set; }
        public string CorrelationId { get; set; }

        /// <summary>Detalle opcional de validación (por campo), solo para HTTP 400.</summary>
        public IEnumerable<string> Errores { get; set; }
    }
}
