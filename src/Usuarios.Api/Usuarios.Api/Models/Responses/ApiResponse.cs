namespace Usuarios.Api.Models.Responses
{
    /// <summary>
    /// Forma de respuesta exitosa definida en la Sección 4 del README.
    /// </summary>
    public class ApiResponse
    {
        public string Codigo { get; set; }
        public string Mensaje { get; set; }
        public string Operacion { get; set; }
        public string CorrelationId { get; set; }
    }
}
