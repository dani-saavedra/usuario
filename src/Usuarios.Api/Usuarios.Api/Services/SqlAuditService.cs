using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Usuarios.Api.Infrastructure;

namespace Usuarios.Api.Services
{
    /// <summary>
    /// Persiste cada entrada de auditoría en dbo.ApiAuditoria (ver
    /// sql/01_create_audit_table.sql). Si la escritura falla -por ejemplo,
    /// SQL Server no disponible- se respalda en archivo local para no perder
    /// el rastro de la solicitud, y nunca se propaga la excepción hacia quien
    /// llamó (Sección 7.3).
    /// </summary>
    public class SqlAuditService : IAuditService
    {
        private readonly string _connectionString;
        private readonly int _commandTimeoutSeconds;

        public SqlAuditService()
        {
            var connectionStringName = AppSettingsHelper.GetString("Auditoria.ConnectionStringName", "UsuariosDb");
            _connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
            _commandTimeoutSeconds = AppSettingsHelper.GetInt("Sql.CommandTimeoutSeconds", 30);
        }

        public async Task LogAsync(AuditEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("No hay cadena de conexión configurada para auditoría.");
                }

                using (var connection = new SqlConnection(_connectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "dbo.usp_RegistrarAuditoria";
                    command.CommandTimeout = _commandTimeoutSeconds;

                    command.Parameters.Add("@FechaHoraUtc", SqlDbType.DateTime2).Value = entry.FechaHoraUtc;
                    command.Parameters.Add("@CorrelationId", SqlDbType.VarChar, 50).Value = (object)entry.CorrelationId ?? DBNull.Value;
                    command.Parameters.Add("@Operacion", SqlDbType.VarChar, 100).Value = (object)entry.Operacion ?? DBNull.Value;
                    command.Parameters.Add("@Consumidor", SqlDbType.VarChar, 100).Value = (object)entry.Consumidor ?? DBNull.Value;
                    command.Parameters.Add("@UsuarioObjetivo", SqlDbType.VarChar, 50).Value = (object)entry.UsuarioObjetivo ?? DBNull.Value;
                    command.Parameters.Add("@Resultado", SqlDbType.VarChar, 20).Value = (object)entry.Resultado ?? DBNull.Value;
                    command.Parameters.Add("@CodigoHttp", SqlDbType.Int).Value = entry.CodigoHttp;
                    command.Parameters.Add("@DuracionMs", SqlDbType.Int).Value = entry.DuracionMs;
                    command.Parameters.Add("@ResultadoSql", SqlDbType.VarChar, 50).Value = (object)entry.ResultadoSql ?? DBNull.Value;
                    command.Parameters.Add("@DetalleError", SqlDbType.NVarChar, 2000).Value = (object)entry.DetalleError ?? DBNull.Value;

                    await connection.OpenAsync().ConfigureAwait(false);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                FileAuditSink.LogAuditFallback(entry.CorrelationId, entry.Operacion,
                    $"Fallo al escribir auditoría en SQL Server. Resultado={entry.Resultado}; CodigoHttp={entry.CodigoHttp}; ResultadoSql={entry.ResultadoSql}; Detalle={entry.DetalleError}",
                    ex);
            }
        }
    }
}
