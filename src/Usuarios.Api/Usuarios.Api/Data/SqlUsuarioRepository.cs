using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api.Data
{
    /// <summary>
    /// Acceso a datos mediante ADO.NET puro, exclusivamente a través de los
    /// procedimientos almacenados autorizados (Sección 3: "La API no debe
    /// actualizar directamente las tablas de negocio"). No se concatenan
    /// cadenas SQL; todos los valores viajan como parámetros tipados
    /// (Sección 8).
    /// </summary>
    public class SqlUsuarioRepository : IUsuarioRepository
    {
        private const int UsuarioMaxLength = 50;
        private const int ResultMessageMaxLength = 200;

        private readonly string _connectionString;
        private readonly int _commandTimeoutSeconds;

        public SqlUsuarioRepository()
        {
            var connectionStringName = AppSettingsHelper.GetString("Sql.ConnectionStringName", "UsuariosDb");
            var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    $"No se encontró la cadena de conexión '{connectionStringName}'.");
            }

            _connectionString = settings.ConnectionString;
            _commandTimeoutSeconds = AppSettingsHelper.GetInt("Sql.CommandTimeoutSeconds", 30);
        }

        public Task<StoredProcedureResult> CerrarSesionYBloquearAsync(string usuario)
        {
            return ExecuteAsync("dbo.usp_CerrarSesionYBloquearUsuario", usuario);
        }

        public Task<StoredProcedureResult> DesbloquearAsync(string usuario)
        {
            return ExecuteAsync("dbo.usp_DesbloquearUsuario", usuario);
        }

        private async Task<StoredProcedureResult> ExecuteAsync(string storedProcedureName, string usuario)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = storedProcedureName;
                    command.CommandTimeout = _commandTimeoutSeconds;

                    command.Parameters.Add("@Usuario", SqlDbType.VarChar, UsuarioMaxLength).Value = usuario;

                    var resultCodeParam = command.Parameters.Add("@ResultCode", SqlDbType.Int);
                    resultCodeParam.Direction = ParameterDirection.Output;

                    var resultMessageParam = command.Parameters.Add("@ResultMessage", SqlDbType.VarChar, ResultMessageMaxLength);
                    resultMessageParam.Direction = ParameterDirection.Output;

                    await connection.OpenAsync().ConfigureAwait(false);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                    var rawCode = resultCodeParam.Value == DBNull.Value ? (int)ResultCode.ErrorEjecucion : (int)resultCodeParam.Value;
                    var message = resultMessageParam.Value == DBNull.Value ? null : (string)resultMessageParam.Value;

                    return StoredProcedureResult.Of(rawCode, message);
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is TimeoutException || ex is InvalidOperationException)
            {
                // InvalidOperationException cubre, entre otros casos, abrir una
                // conexión ya abierta o un pool agotado; se trata igual que una
                // falla de infraestructura de base de datos.
                throw new DataAccessException($"Error al ejecutar {storedProcedureName}", ex);
            }
        }
    }
}
