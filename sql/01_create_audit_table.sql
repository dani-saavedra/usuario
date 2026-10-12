/*
================================================================================
 01_create_audit_table.sql

 Tabla de auditoría (Sección 7) y procedimiento para insertar en ella.
 La API nunca hace INSERT directo: siempre pasa por
 dbo.usp_RegistrarAuditoria, de modo que el usuario SQL de la API solo
 necesite permiso EXECUTE (Sección 6.2) y nunca permisos directos de
 escritura sobre tablas de negocio ni de auditoría.

 Idempotente: puede ejecutarse varias veces sin error.
================================================================================
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.ApiAuditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiAuditoria
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL,
        FechaHoraUtc     DATETIME2(3)          NOT NULL,
        CorrelationId    VARCHAR(50)           NOT NULL,
        Operacion        VARCHAR(100)          NOT NULL,
        Consumidor       VARCHAR(100)          NULL,
        UsuarioObjetivo  VARCHAR(50)           NULL,
        Resultado        VARCHAR(20)           NOT NULL,
        CodigoHttp       INT                   NOT NULL,
        DuracionMs       INT                   NOT NULL,
        ResultadoSql     VARCHAR(50)           NULL,
        DetalleError     NVARCHAR(2000)        NULL,
        CONSTRAINT PK_ApiAuditoria PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_ApiAuditoria_FechaHoraUtc ON dbo.ApiAuditoria (FechaHoraUtc);
    CREATE INDEX IX_ApiAuditoria_CorrelationId ON dbo.ApiAuditoria (CorrelationId);
    CREATE INDEX IX_ApiAuditoria_UsuarioObjetivo ON dbo.ApiAuditoria (UsuarioObjetivo);
END
GO

IF OBJECT_ID('dbo.usp_RegistrarAuditoria', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RegistrarAuditoria;
GO

CREATE PROCEDURE dbo.usp_RegistrarAuditoria
(
    @FechaHoraUtc     DATETIME2(3),
    @CorrelationId    VARCHAR(50),
    @Operacion        VARCHAR(100),
    @Consumidor       VARCHAR(100)   = NULL,
    @UsuarioObjetivo  VARCHAR(50)    = NULL,
    @Resultado        VARCHAR(20),
    @CodigoHttp       INT,
    @DuracionMs       INT,
    @ResultadoSql     VARCHAR(50)    = NULL,
    @DetalleError     NVARCHAR(2000) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ApiAuditoria
    (
        FechaHoraUtc, CorrelationId, Operacion, Consumidor, UsuarioObjetivo,
        Resultado, CodigoHttp, DuracionMs, ResultadoSql, DetalleError
    )
    VALUES
    (
        @FechaHoraUtc, @CorrelationId, @Operacion, @Consumidor, @UsuarioObjetivo,
        @Resultado, @CodigoHttp, @DuracionMs, @ResultadoSql, @DetalleError
    );
END
GO
