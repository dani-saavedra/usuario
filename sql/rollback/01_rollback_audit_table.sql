/*
  Reversión de 01_create_audit_table.sql.
  ADVERTENCIA: elimina permanentemente el historial de auditoría. Solo
  ejecutar si se decide descontinuar la API o re-crear el esquema desde cero
  en un ambiente no productivo. En producción, preferir archivar
  dbo.ApiAuditoria antes de eliminarla (política de retención, Sección 7.3).
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_RegistrarAuditoria', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RegistrarAuditoria;
GO

IF OBJECT_ID('dbo.ApiAuditoria', 'U') IS NOT NULL
    DROP TABLE dbo.ApiAuditoria;
GO
