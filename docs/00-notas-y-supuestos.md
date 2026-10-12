# Notas y supuestos de esta implementación

Este documento resume las decisiones tomadas para poder generar un entregable
completo a partir de `README.md`, y lo que **debe validarse antes de
producción** según la Sección 5.4 y la sección "Consideraciones pendientes
antes de construir" del propio README.

## 1. Esquema de base de datos (pendiente de validación)

Se confirmaron únicamente los nombres de tabla `usuario` y `sesion`. Las
columnas, tipos de dato y la relación entre ambas tablas **son una
suposición** documentada en la cabecera de:

- `sql/00_esquema_prueba_opcional.sql`
- `sql/02_create_usp_CerrarSesionYBloquearUsuario.sql`
- `sql/03_create_usp_DesbloquearUsuario.sql`

Resumen de la suposición:

| Tabla     | Columna               | Supuesto                                   |
| --------- | --------------------- | ------------------------------------------- |
| usuario   | `id_usuario`          | PK interna, INT IDENTITY                    |
| usuario   | `usuario`              | Identificador funcional/login (UNIQUE), el mismo que recibe la API en el JSON |
| usuario   | `estado`               | CHAR(1), `V` vigente / `I` inactivo         |
| usuario   | `fecha_actualizacion`  | DATETIME2, auditoría interna de la tabla    |
| sesion    | `id_usuario`           | FK -> usuario.id_usuario                    |
| sesion    | `estado`               | VARCHAR(10), `ACTIVA` / `INACTIVA`          |
| sesion    | `fecha_fin`            | DATETIME2 NULL, se completa al invalidar    |

**Antes de pasar a producción**, un responsable del esquema real debe:

1. Confirmar o corregir los nombres/tipos anteriores.
2. Confirmar si existen múltiples aplicaciones/canales en `sesion` que deban
   tratarse de forma distinta (el diseño actual invalida **todas** las
   sesiones del usuario, sin distinguir canal).
3. Ajustar `02_create_usp_CerrarSesionYBloquearUsuario.sql` y
   `03_create_usp_DesbloquearUsuario.sql` en consecuencia.
4. Ajustar la validación de formato de `Models/Requests/UsuarioOperationRequest.cs`
   (longitud máxima y expresión regular) si el identificador real tiene otro
   formato.

Sin este paso, **no ejecutar los scripts 02 y 03 contra una base de datos de
producción real** — sólo se validaron contra el esquema de prueba de
`00_esquema_prueba_opcional.sql`.

## 2. Gestión de secretos

Se optó por el mecanismo estándar de .NET Framework / IIS:

- Las API Keys viven en `Secrets/ApiKeys.config` (archivo externo referenciado
  desde `Web.config` vía `<appSettings file="Secrets\ApiKeys.config">`), no en
  el propio `Web.config`.
- La cadena de conexión vive en `connectionStrings` de `Web.config`.
- Ambas secciones se protegen en el servidor mediante `aspnet_regiis -pef`
  (ver `docs/02-instructivo-despliegue-iis.md`).

Si la organización ya cuenta con un gestor de secretos corporativo (Azure Key
Vault, HashiCorp Vault, etc.), `Services/ApiKeyProvider.cs` y
`Data/SqlUsuarioRepository.cs` son los dos únicos puntos que habría que
adaptar para leer desde ese gestor en lugar de `ConfigurationManager`.

## 3. Mecanismo de auditoría

Primario: tabla `dbo.ApiAuditoria` vía `dbo.usp_RegistrarAuditoria` (mismo
servidor SQL que la base de datos de negocio, configurable de forma
independiente con `Auditoria.ConnectionStringName`).

Respaldo: si la escritura en SQL Server falla, se escribe en un archivo plano
bajo `App_Data\Logs\` (ver `Infrastructure/FileAuditSink.cs`). Este respaldo
es deliberadamente simple (sin rotación por tamaño, sin compresión) porque es
un mecanismo de última instancia, no el primario; si en la práctica se usa con
frecuencia, es señal de que la disponibilidad de SQL Server para auditoría debe
revisarse.

## 4. Decisiones no especificadas explícitamente en el README

- El campo `motivo` se implementó como **opcional** (Sección 4.1: "Su
  obligatoriedad deberá definirse según las reglas del negocio"). Se recibe y
  valida la longitud, pero no se persiste porque el README no define dónde
  almacenarlo; si el negocio decide guardarlo, se debe agregar una columna al
  esquema real y pasarlo como parámetro a ambos procedimientos almacenados.
- Una solicitud repetida de bloqueo sobre un usuario ya bloqueado es
  idempotente y responde `200 OK` (no hay nada más que invalidar). Una
  solicitud repetida de desbloqueo sobre un usuario ya vigente también
  responde `200 OK`. Esta interpretación de "resultado determinista" debe
  confirmarse con negocio si se espera un comportamiento distinto (por
  ejemplo, `409 Conflict`).
- El `correlationId` que el consumidor puede enviar en `X-Correlation-Id` sólo
  se acepta si tiene forma de GUID; en cualquier otro caso se genera uno
  nuevo, para evitar que valores arbitrarios lleguen a la tabla de auditoría.
