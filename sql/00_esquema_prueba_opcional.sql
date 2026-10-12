/*
================================================================================
 00_esquema_prueba_opcional.sql

 PROPÓSITO
 ---------
 Este script NO es parte del esquema de producción. Crea una versión mínima
 de las tablas "usuario" y "sesion" únicamente para poder probar de punta a
 punta los procedimientos almacenados (02 y 03) y la API en un ambiente de
 desarrollo o pruebas (Sección 9: "Crear y validar los procedimientos
 almacenados en un ambiente no productivo").

 SUPOSICIÓN DE ESQUEMA — DEBE VALIDARSE ANTES DE PRODUCCIÓN
 ------------------------------------------------------------
 El README (Sección 5.4) es explícito: no deben asumirse nombres de columna
 ni relaciones no confirmadas contra el esquema real. Al momento de generar
 este entregable solo se confirmaron los NOMBRES de tabla ("usuario" y
 "sesion"); las columnas que siguen son una suposición razonable y DEBEN
 contrastarse con el esquema real antes de ejecutar 02_* y 03_* en
 producción:

   usuario
     - id_usuario     INT IDENTITY       (clave primaria interna)
     - usuario        VARCHAR(50) UNIQUE (identificador funcional / login,
                                           el mismo que viaja en el JSON de
                                           la API como "usuario")
     - estado         CHAR(1)            ('V' = vigente, 'I' = inactivo)
     - fecha_actualizacion DATETIME2

   sesion
     - id_sesion       INT IDENTITY
     - id_usuario      INT               (FK -> usuario.id_usuario)
     - estado          VARCHAR(10)       ('ACTIVA', 'INACTIVA')
     - fecha_inicio     DATETIME2
     - fecha_fin        DATETIME2 NULL

 Si el esquema real difiere (otros nombres, otro tipo de dato para el
 identificador, relación N:M con aplicaciones/canales, etc.), este script y
 los procedimientos 02/03 deben ajustarse antes de usarse contra la base de
 datos real. NO ejecutar este script contra una base de datos que ya tenga
 tablas "usuario"/"sesion" reales.
================================================================================
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.sesion', 'U') IS NOT NULL OR OBJECT_ID('dbo.usuario', 'U') IS NOT NULL
BEGIN
    RAISERROR('Ya existen objetos dbo.usuario o dbo.sesion. Este script es solo para una base de datos de prueba vacía; revíselo manualmente antes de continuar.', 16, 1);
    RETURN;
END
GO

CREATE TABLE dbo.usuario
(
    id_usuario           INT IDENTITY(1,1)   NOT NULL,
    usuario              VARCHAR(50)         NOT NULL,
    estado               CHAR(1)             NOT NULL CONSTRAINT DF_usuario_estado DEFAULT ('V'),
    fecha_actualizacion  DATETIME2(0)        NULL,
    CONSTRAINT PK_usuario PRIMARY KEY CLUSTERED (id_usuario),
    CONSTRAINT UQ_usuario_usuario UNIQUE (usuario),
    CONSTRAINT CK_usuario_estado CHECK (estado IN ('V', 'I'))
);
GO

CREATE TABLE dbo.sesion
(
    id_sesion     INT IDENTITY(1,1)  NOT NULL,
    id_usuario    INT                NOT NULL,
    estado        VARCHAR(10)        NOT NULL CONSTRAINT DF_sesion_estado DEFAULT ('ACTIVA'),
    fecha_inicio  DATETIME2(0)       NOT NULL CONSTRAINT DF_sesion_fecha_inicio DEFAULT (SYSUTCDATETIME()),
    fecha_fin     DATETIME2(0)       NULL,
    CONSTRAINT PK_sesion PRIMARY KEY CLUSTERED (id_sesion),
    CONSTRAINT FK_sesion_usuario FOREIGN KEY (id_usuario) REFERENCES dbo.usuario (id_usuario),
    CONSTRAINT CK_sesion_estado CHECK (estado IN ('ACTIVA', 'INACTIVA'))
);
GO

CREATE INDEX IX_sesion_id_usuario_estado ON dbo.sesion (id_usuario, estado);
GO

-- Datos mínimos de ejemplo para pruebas manuales / casos de aceptación (Sección 10).
INSERT INTO dbo.usuario (usuario, estado) VALUES ('usuario.prueba1', 'V');
INSERT INTO dbo.usuario (usuario, estado) VALUES ('usuario.prueba2', 'V');
GO

INSERT INTO dbo.sesion (id_usuario, estado)
SELECT id_usuario, 'ACTIVA' FROM dbo.usuario WHERE usuario = 'usuario.prueba1';
GO
