using System.Threading.Tasks;

namespace Usuarios.Api.Data
{
    public interface IUsuarioRepository
    {
        /// <summary>
        /// Ejecuta dbo.usp_CerrarSesionYBloquearUsuario. Puede lanzar
        /// <see cref="DataAccessException"/> si SQL Server no responde.
        /// </summary>
        Task<StoredProcedureResult> CerrarSesionYBloquearAsync(string usuario);

        /// <summary>
        /// Ejecuta dbo.usp_DesbloquearUsuario. Puede lanzar
        /// <see cref="DataAccessException"/> si SQL Server no responde.
        /// </summary>
        Task<StoredProcedureResult> DesbloquearAsync(string usuario);
    }
}
