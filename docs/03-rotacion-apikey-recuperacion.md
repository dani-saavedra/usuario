# Rotación de API Key y recuperación ante fallos

## 1. Rotación de API Key (sin interrupción de servicio)

La API lee las claves en cada solicitud desde `ConfigurationManager.AppSettings`
(`Services/ApiKeyProvider.cs`); no las cachea en memoria más allá de lo que
ASP.NET ya hace vía `FileSystemWatcher` sobre `Web.config` y sus archivos
`file=` referenciados. Esto permite rotar sin detener el Application Pool,
aunque sí provoca un reciclaje automático del AppDomain (igual que cualquier
cambio de `Web.config`).

Pasos:

1. Generar una nueva clave segura (ejemplo en PowerShell):
   ```powershell
   [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
   ```
2. Añadir la nueva clave **junto a** la anterior, con un sufijo temporal, para
   permitir una ventana de convivencia:
   ```xml
   <add key="ApiKey:SistemaFacturacion"        value="<clave-anterior>" />
   <add key="ApiKey:SistemaFacturacion.v2"     value="<clave-nueva>" />
   ```
   > El nombre después de `ApiKey:` es el "nombre de consumidor" que queda
   > registrado en auditoría (`Consumidor`); usar un sufijo de versión es
   > aceptable y queda trazado como un consumidor lógico distinto durante la
   > transición.
3. Entregar la clave nueva al equipo consumidor y coordinar la fecha de corte.
4. Confirmar en `dbo.ApiAuditoria` que ya no llegan llamadas con el
   consumidor antiguo (`Consumidor = 'SistemaFacturacion'`).
5. Eliminar la entrada antigua de `Secrets\ApiKeys.config` y, si se desea,
   renombrar `SistemaFacturacion.v2` a `SistemaFacturacion`.

En cada paso que modifique `Secrets\ApiKeys.config`, `StartupValidation`
(`Infrastructure/StartupValidation.cs`) validará al próximo arranque del
AppDomain que no haya claves duplicadas entre consumidores; si la hay, la
aplicación falla al iniciar antes de aceptar tráfico, en vez de aceptar una
clave ambigua.

### Rotación de emergencia (clave comprometida)

1. Eliminar inmediatamente la entrada comprometida de `Secrets\ApiKeys.config`
   (no esperar a coordinar con el consumidor: una clave comprometida no debe
   seguir siendo válida).
2. Guardar el archivo; ASP.NET reciclará el AppDomain y la clave dejará de
   aceptarse en la siguiente solicitud.
3. Revisar `dbo.ApiAuditoria` filtrando por el consumidor afectado, en el
   rango de tiempo desde la sospecha de compromiso, para evaluar impacto
   (qué usuarios fueron bloqueados/desbloqueados con esa clave).
4. Emitir una clave nueva siguiendo el procedimiento normal de rotación.
5. Documentar el incidente según el proceso de gestión de incidentes de la
   organización.

## 2. Recuperación ante fallos

### SQL Server no disponible

Síntoma: la API responde `500` con `codigo: "ERROR_INTERNO"` en todas las
solicitudes, y aparecen entradas en `App_Data\Logs\audit-fallback-*.log` con
`ResultadoSql=SQL_NO_DISPONIBLE` o similar.

1. Confirmar conectividad de red IIS → SQL Server (puerto, firewall).
2. Confirmar que el servicio de SQL Server está arriba y la base de datos en
   línea (`sys.databases.state_desc = 'ONLINE'`).
3. Confirmar que el login de la API no está bloqueado/expirado
   (`sys.sql_logins`, `LOGINPROPERTY('<login>', 'IsLocked')`).
4. Una vez restablecida la conectividad, la API se recupera sola en la
   siguiente solicitud (no requiere reinicio) porque `SqlConnection` abre una
   conexión nueva por cada operación.
5. Recuperar del archivo de respaldo (`App_Data\Logs\audit-fallback-*.log`)
   las operaciones que no llegaron a quedar en `dbo.ApiAuditoria` durante la
   ventana de indisponibilidad, si se requiere reconstruir el historial.

### Aplicación no arranca después de un despliegue

`Global.asax.cs` llama a `StartupValidation.EnsureRequiredConfiguration()` en
`Application_Start`, que lanza una excepción (visible en el Event Viewer de
Windows, origen ASP.NET, y en IIS como `500.19`/`500.0` al primer request) si:

- Falta o está vacía la cadena de conexión `UsuariosDb`.
- No hay ninguna API Key configurada en `Secrets\ApiKeys.config`.
- Hay API Keys duplicadas entre distintos consumidores.

Esto es intencional (Sección 12 del README: la aplicación no debe aceptar
tráfico sin poder validar la API Key ni sin acceso a SQL Server configurado).
Revisar el mensaje de la excepción en el Event Viewer, corregir
`Web.config`/`Secrets\ApiKeys.config`, y volver a intentar (no requiere
recompilar).

### Pérdida de `Secrets\ApiKeys.config`

Si el archivo se borra o corrompe, la aplicación dejará de arrancar (ver caso
anterior). Mantener una copia de respaldo fuera del servidor (gestor de
secretos, bóveda de contraseñas corporativa) — **nunca** en el repositorio de
código fuente en texto plano.

### Rollback de una versión con errores

Ver Sección 10 de `docs/02-instructivo-despliegue-iis.md`.
