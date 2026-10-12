/* Reversión de 02_create_usp_CerrarSesionYBloquearUsuario.sql */
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_CerrarSesionYBloquearUsuario', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CerrarSesionYBloquearUsuario;
GO
