/* Reversión de 04_grants.sql */
SET NOCOUNT ON;
GO

IF DATABASE_PRINCIPAL_ID('rol_usuarios_api') IS NOT NULL
BEGIN
    REVOKE EXECUTE ON dbo.usp_CerrarSesionYBloquearUsuario FROM rol_usuarios_api;
    REVOKE EXECUTE ON dbo.usp_DesbloquearUsuario FROM rol_usuarios_api;
    REVOKE EXECUTE ON dbo.usp_RegistrarAuditoria FROM rol_usuarios_api;
    DROP ROLE rol_usuarios_api;
END
GO
