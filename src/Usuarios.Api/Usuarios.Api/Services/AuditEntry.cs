using System;

namespace Usuarios.Api.Services
{
    /// <summary>
    /// Campos mínimos de auditoría definidos en la Sección 7.1 del README.
    /// </summary>
    public class AuditEntry
    {
        public DateTime FechaHoraUtc { get; set; } = DateTime.UtcNow;
        public string CorrelationId { get; set; }
        public string Operacion { get; set; }
        public string Consumidor { get; set; }
        public string UsuarioObjetivo { get; set; }
        public string Resultado { get; set; }
        public int CodigoHttp { get; set; }
        public long DuracionMs { get; set; }
        public string ResultadoSql { get; set; }
        public string DetalleError { get; set; }
    }
}
