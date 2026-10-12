/* Reversión de 03_create_usp_DesbloquearUsuario.sql */
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_DesbloquearUsuario', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DesbloquearUsuario;
GO
