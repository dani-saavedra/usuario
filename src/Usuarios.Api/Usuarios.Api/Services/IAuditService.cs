using System.Threading.Tasks;

namespace Usuarios.Api.Services
{
    public interface IAuditService
    {
        /// <summary>
        /// Nunca debe lanzar: una falla al auditar no debe impedir ni alterar
        /// la respuesta de la operación de negocio (Sección 7.3). La
        /// implementación es responsable de resolver internamente cualquier
        /// mecanismo de respaldo.
        /// </summary>
        Task LogAsync(AuditEntry entry);
    }
}
