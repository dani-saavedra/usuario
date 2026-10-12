# Instructivo de despliegue y configuración en IIS 10

Sigue el orden de las secciones. Cada paso indica si se ejecuta en la
estación de compilación, en el servidor SQL o en el servidor IIS.

## 1. Prerrequisitos en el servidor IIS

- Windows Server con IIS 10 ya instalado (existente, según Sección 2 del README).
- Feature de ASP.NET 4.8 habilitado:
  ```powershell
  Install-WindowsFeature Web-Asp-Net45
  ```
  o, si no se usa el módulo de PowerShell de Windows Features:
  ```cmd
  %windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -i
  ```
- Verificar que el servidor pueda alcanzar la instancia de SQL Server 2022 en
  el puerto configurado (1433 u otro), y que exista un login/usuario SQL
  dedicado para la API (Sección 6.2).

## 2. Compilación

En una estación con Visual Studio 2019/2022 (o build agent con MSBuild +
NuGet):

```cmd
nuget restore src\Usuarios.Api\Usuarios.Api.sln
msbuild src\Usuarios.Api\Usuarios.Api.sln /p:Configuration=Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile /p:publishUrl=publish\Usuarios.Api
```

Si no se dispone de un `PublishProfile`, usar el asistente de Visual Studio:
botón derecho sobre `Usuarios.Api` → Publish → File System → carpeta
`publish\Usuarios.Api`, en configuración `Release`.

Correr las pruebas antes de publicar:

```cmd
nuget restore tests\Usuarios.Api.Tests\Usuarios.Api.Tests.csproj
msbuild tests\Usuarios.Api.Tests\Usuarios.Api.Tests.csproj /p:Configuration=Release
vstest.console.exe tests\Usuarios.Api.Tests\bin\Release\Usuarios.Api.Tests.dll
```

## 3. Base de datos (ambiente no productivo primero)

Ejecutar en orden, en un ambiente de pruebas (Sección 9, paso 5):

1. `sql/00_esquema_prueba_opcional.sql` — **solo** en bases de datos de
   prueba vacías, para validar funcionalmente antes de tener el esquema real.
2. `sql/01_create_audit_table.sql`
3. `sql/02_create_usp_CerrarSesionYBloquearUsuario.sql`
4. `sql/03_create_usp_DesbloquearUsuario.sql`
5. `sql/04_grants.sql` (ajustar `<USUARIO_API>` al login real y descomentar
   el `ALTER ROLE ... ADD MEMBER`)

Repetir 1–5 (sin el paso 1) contra el esquema real una vez validado contra
`docs/00-notas-y-supuestos.md`.

## 4. Publicación de archivos en el servidor

1. Copiar el contenido de `publish\Usuarios.Api\` a una carpeta dedicada,
   por ejemplo `D:\Apps\UsuariosApi\`. **No reutilizar** la carpeta de otra
   aplicación existente (Sección 9: "no debe alterar... rutas de las
   aplicaciones existentes").
2. Copiar `Secrets\ApiKeys.config.sample` como `Secrets\ApiKeys.config`
   **directamente en el servidor** (no versionar el archivo con claves
   reales) y completar una clave distinta por cada consumidor autorizado.
3. Editar `connectionStrings` en `Web.config` con el servidor, base de
   datos y credenciales reales de SQL Server.

## 5. Protección de configuración sensible

Desde una consola elevada en el servidor, situado en
`%windir%\Microsoft.NET\Framework64\v4.0.30319\`:

```cmd
aspnet_regiis -pef "connectionStrings" "D:\Apps\UsuariosApi"
```

Esto cifra la cadena de conexión dentro de `Web.config` usando el proveedor
DPAPI a nivel de máquina (solo se puede descifrar en ese mismo servidor).

> **Nota sobre `Secrets\ApiKeys.config`:** por ser un archivo externo referenciado
> vía `<appSettings file="...">`, `aspnet_regiis` no puede cifrar su contenido
> directamente (solo cifraría la referencia dentro de `Web.config`, no el
> archivo en sí). Su protección se basa en permisos de archivo (paso 6) y en
> que IIS, por configuración predeterminada, nunca sirve archivos `.config`
> por HTTP. Si la organización requiere cifrado en disco también para este
> archivo, evaluar un gestor de secretos corporativo (ver
> `docs/00-notas-y-supuestos.md`, sección 2).

Para revertir el cifrado (por ejemplo, antes de mover la app a otro
servidor):

```cmd
aspnet_regiis -pdf "connectionStrings" "D:\Apps\UsuariosApi"
```

## 6. Permisos de archivos (NTFS)

Identidad del Application Pool (ver paso 7): por defecto
`IIS AppPool\UsuariosApiPool`.

```powershell
$appPath = "D:\Apps\UsuariosApi"
icacls $appPath /grant "IIS AppPool\UsuariosApiPool:(OI)(CI)RX"
icacls "$appPath\App_Data\Logs" /grant "IIS AppPool\UsuariosApiPool:(OI)(CI)M"
icacls "$appPath\Secrets" /grant "IIS AppPool\UsuariosApiPool:(OI)(CI)R"
icacls "$appPath\Secrets" /inheritance:r /grant "Administrators:(OI)(CI)F" /grant "IIS AppPool\UsuariosApiPool:(OI)(CI)R"
```

Crear `App_Data\Logs` si no existe (debe poder escribir el Application Pool,
es el respaldo de auditoría — Sección 7.3).

## 7. Application Pool

Crear un Application Pool dedicado (no reutilizar uno existente):

| Propiedad                  | Valor                              |
| --------------------------- | ----------------------------------- |
| Nombre                      | `UsuariosApiPool`                   |
| .NET CLR Version            | `v4.0`                              |
| Managed Pipeline Mode       | `Integrated`                        |
| Identity                    | `ApplicationPoolIdentity` (o cuenta de servicio dedicada según política) |
| Enable 32-Bit Applications  | `False`                             |
| Start Mode                  | `AlwaysRunning`                     |
| Idle Time-out (minutes)     | `0` (o alto, según política de recursos) |

```powershell
Import-Module WebAdministration
New-WebAppPool -Name "UsuariosApiPool"
Set-ItemProperty IIS:\AppPools\UsuariosApiPool managedRuntimeVersion v4.0
Set-ItemProperty IIS:\AppPools\UsuariosApiPool startMode AlwaysRunning
Set-ItemProperty IIS:\AppPools\UsuariosApiPool processModel.idleTimeout "00:00:00"
```

## 8. Aplicación en IIS

Crear la aplicación como una ruta independiente bajo un sitio existente (o un
sitio nuevo si así lo define la organización), **nunca** anidada dentro de
otra aplicación existente:

```powershell
New-WebApplication -Site "Default Web Site" -Name "usuarios-api" `
  -PhysicalPath "D:\Apps\UsuariosApi" -ApplicationPool "UsuariosApiPool"
```

Configurar el binding HTTPS del sitio con un certificado válido; no exponer
el endpoint solo por HTTP en producción.

## 9. Verificación post-despliegue

```cmd
curl -i -X POST https://<host>/usuarios-api/api/v1/usuarios/desbloquear ^
  -H "Content-Type: application/json" ^
  -H "X-API-Key: <clave-real-de-prueba>" ^
  -d "{\"usuario\":\"usuario.prueba1\"}"
```

Validar, en este orden:

1. Sin `X-API-Key` → `401`.
2. Con `X-API-Key` inválida → `401`.
3. Con `X-API-Key` válida y usuario inexistente → `404`.
4. Con `X-API-Key` válida y usuario existente → `200`.
5. Revisar `dbo.ApiAuditoria` y confirmar que las 4 llamadas anteriores
   quedaron registradas (Sección 7.2).
6. Revisar que `App_Data\Logs\` **no** tenga archivos `audit-fallback-*.log`
   recientes (si los hay, la escritura en SQL Server está fallando).

Ver también la colección de Postman en `postman/Usuarios-Api.postman_collection.json`
y la matriz completa de casos en `docs/04-plan-pruebas-aceptacion.md`.

## 10. Reversión (rollback)

**Aplicación:**
1. Detener el Application Pool: `Stop-WebAppPool -Name "UsuariosApiPool"`.
2. Restaurar la carpeta de publicación de la versión anterior (mantener
   siempre un respaldo con timestamp antes de sobrescribir, por ejemplo
   `D:\Apps\UsuariosApi.bak.<fecha>`).
3. Reiniciar el Application Pool.

**Base de datos**, solo si la versión que se revierte cambió el contrato de
los procedimientos:
```cmd
sqlcmd -S <servidor> -d <basededatos> -i sql\rollback\04_rollback_grants.sql
sqlcmd -S <servidor> -d <basededatos> -i sql\rollback\03_rollback_usp_DesbloquearUsuario.sql
sqlcmd -S <servidor> -d <basededatos> -i sql\rollback\02_rollback_usp_CerrarSesionYBloquearUsuario.sql
sqlcmd -S <servidor> -d <basededatos> -i sql\rollback\01_rollback_audit_table.sql
```
Los scripts de reversión **eliminan los procedimientos y, si se ejecuta el
01, la tabla de auditoría completa** — no revierten datos de `usuario` ni
`sesion` modificados por llamadas ya ejecutadas (eso no es un "rollback"
transaccional retroactivo; es responsabilidad del proceso de cambios de la
organización decidir si se requiere restaurar esas tablas desde un backup).

## 11. Observabilidad continua

- `dbo.ApiAuditoria`: fuente primaria para soporte/auditoría (Sección 7).
- `App_Data\Logs\audit-fallback-*.log` y `host-error-*.log`: revisar
  periódicamente; su presencia indica incidentes (SQL Server no disponible,
  o errores fuera del pipeline de Web API).
- Logs de IIS (`%SystemDrive%\inetpub\logs\LogFiles`): complementan con el
  detalle HTTP crudo (Sección 7.3).
