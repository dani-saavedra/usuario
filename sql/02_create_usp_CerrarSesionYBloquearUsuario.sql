/*
================================================================================
 02_create_usp_CerrarSesionYBloquearUsuario.sql

 Implementa la Sección 5.2 del README.

 CONTRATO (debe coincidir con Usuarios.Api/Data/ResultCode.cs):
   @ResultCode = 0  -> OK: sesiones invalidadas y usuario en estado 'I'
   @ResultCode = 1  -> el usuario no existe; no se modifica nada
   @ResultCode = 2  -> error de ejecución; la transacción se revirtió

 SUPOSICIÓN DE ESQUEMA (Sección 5.4) — validar antes de producción:
   dbo.usuario(id_usuario PK, usuario UNIQUE, estado CHAR(1) IN ('V','I'))
   dbo.sesion(id_usuario FK -> usuario.id_usuario, estado VARCHAR(10))
 Ver sql/00_esquema_prueba_opcional.sql para el detalle completo de la
 suposición. Si el esquema real usa otros nombres de columna, otro tipo de
 dato para el identificador de usuario, o requiere distinguir sesiones por
 aplicación/canal, este procedimiento debe ajustarse en consecuencia.

 Idempotente: puede ejecutarse varias veces (DROP + CREATE).
================================================================================
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_CerrarSesionYBloquearUsuario', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CerrarSesionYBloquearUsuario;
GO

CREATE PROCEDURE dbo.usp_CerrarSesionYBloquearUsuario
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
        -- 1) Verificar existencia del usuario (Sección 5.2, responsabilidad 2).
        SELECT @IdUsuario = id_usuario
        FROM dbo.usuario
        WHERE usuario = @Usuario;

        IF @IdUsuario IS NULL
        BEGIN
            SET @ResultCode = 1;
            SET @ResultMessage = 'El usuario no existe';
            RETURN;
        END

        BEGIN TRANSACTION;

            -- 2) Invalidar las sesiones que correspondan (idempotente: una
            --    sesión ya INACTIVA no se vuelve a tocar, por lo que una
            --    solicitud repetida no produce efectos adicionales).
            UPDATE dbo.sesion
            SET estado = 'INACTIVA',
                fecha_fin = SYSUTCDATETIME()
            WHERE id_usuario = @IdUsuario
              AND estado <> 'INACTIVA';

            -- 3) Bloquear al usuario.
            UPDATE dbo.usuario
            SET estado = 'I',
                fecha_actualizacion = SYSUTCDATETIME()
            WHERE id_usuario = @IdUsuario;

        COMMIT TRANSACTION;

        SET @ResultCode = 0;
        SET @ResultMessage = 'OK';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @ResultCode = 2;
        SET @ResultMessage = LEFT(ERROR_MESSAGE(), 200);
    END CATCH
END
GO
