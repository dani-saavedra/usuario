/*
================================================================================
 04_grants.sql

 Otorga únicamente permiso EXECUTE sobre los procedimientos autorizados
 (Sección 6.2: "Otorgar únicamente permisos EXECUTE sobre los procedimientos
 almacenados autorizados" / "Evitar permisos directos de actualización sobre
 las tablas de negocio"). El usuario de SQL que usa la API NO debe recibir
 GRANT SELECT/INSERT/UPDATE directo sobre dbo.usuario, dbo.sesion ni
 dbo.ApiAuditoria.

 Reemplazar <USUARIO_API> por el login/usuario real de SQL Server que
 configure la cadena de conexión de la API (Web.config, Sección 6.2).
================================================================================
*/

SET NOCOUNT ON;
GO

IF DATABASE_PRINCIPAL_ID('rol_usuarios_api') IS NULL
BEGIN
    CREATE ROLE rol_usuarios_api;
END
GO

GRANT EXECUTE ON dbo.usp_CerrarSesionYBloquearUsuario TO rol_usuarios_api;
GRANT EXECUTE ON dbo.usp_DesbloquearUsuario TO rol_usuarios_api;
GRANT EXECUTE ON dbo.usp_RegistrarAuditoria TO rol_usuarios_api;
GO

-- Agregar el usuario de base de datos de la API al rol.
-- El usuario de base de datos debe existir previamente (CREATE USER
-- <USUARIO_API> FOR LOGIN <USUARIO_API>), lo cual es responsabilidad del
-- equipo de DBA/infraestructura según el proceso estándar de la organización.
-- ALTER ROLE rol_usuarios_api ADD MEMBER [<USUARIO_API>];

-- Verificación recomendada después de ejecutar este script:
--   SELECT dp.name AS principal, o.name AS objeto, p.permission_name
--   FROM sys.database_permissions p
--   JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
--   JOIN sys.objects o ON o.object_id = p.major_id
--   WHERE dp.name = 'rol_usuarios_api';
