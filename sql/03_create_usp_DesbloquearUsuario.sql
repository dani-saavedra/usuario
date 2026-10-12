/*
================================================================================
 03_create_usp_DesbloquearUsuario.sql

 Implementa la Sección 5.3 del README.

 CONTRATO (debe coincidir con Usuarios.Api/Data/ResultCode.cs):
   @ResultCode = 0  -> OK: usuario en estado 'V'
   @ResultCode = 1  -> el usuario no existe; no se modifica nada
   @ResultCode = 2  -> error de ejecución

 Este procedimiento NO debe tocar dbo.sesion bajo ninguna circunstancia
 (Sección 4.2 / 5.3: desbloquear no reactiva sesiones cerradas).

 SUPOSICIÓN DE ESQUEMA (Sección 5.4): ver cabecera de
 sql/02_create_usp_CerrarSesionYBloquearUsuario.sql.

 Idempotente: puede ejecutarse varias veces (DROP + CREATE).
================================================================================
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_DesbloquearUsuario', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DesbloquearUsuario;
GO

CREATE PROCEDURE dbo.usp_DesbloquearUsuario
(
    @Usuario       VARCHAR(50),
    @ResultCode    INT             OUTPUT,
    @ResultMessage VARCHAR(200)    OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdUsuario INT;

    BEGIN TRY
        SELECT @IdUsuario = id_usuario
        FROM dbo.usuario
        WHERE usuario = @Usuario;

        IF @IdUsuario IS NULL
        BEGIN
            SET @ResultCode = 1;
            SET @ResultMessage = 'El usuario no existe';
            RETURN;
        END

        -- Única modificación permitida: el estado del usuario. No se toca
        -- dbo.sesion (Sección 5.3).
        UPDATE dbo.usuario
        SET estado = 'V',
            fecha_actualizacion = SYSUTCDATETIME()
        WHERE id_usuario = @IdUsuario;

        SET @ResultCode = 0;
        SET @ResultMessage = 'OK';
    END TRY
    BEGIN CATCH
        SET @ResultCode = 2;
        SET @ResultMessage = LEFT(ERROR_MESSAGE(), 200);
    END CATCH
END
GO
