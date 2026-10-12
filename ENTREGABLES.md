# Entregables (Sección 11 del README)

| Entregable | Ubicación |
| ----------- | --------- |
| Código fuente de la API REST | `src/Usuarios.Api/` |
| Solución de Visual Studio (.NET Framework 4.8) | `src/Usuarios.Api/Usuarios.Api.sln` |
| Dos procedimientos almacenados documentados | `sql/02_create_usp_CerrarSesionYBloquearUsuario.sql`, `sql/03_create_usp_DesbloquearUsuario.sql` |
| Configuración de conexión a SQL Server | `src/Usuarios.Api/Usuarios.Api/Web.config` (`connectionStrings`) |
| Mecanismo de validación de API Key | `src/Usuarios.Api/Usuarios.Api/Handlers/ApiKeyAuthHandler.cs`, `Services/ApiKeyProvider.cs`, `Secrets/ApiKeys.config.sample` |
| Implementación de auditoría | `Services/SqlAuditService.cs`, `Infrastructure/FileAuditSink.cs`, `sql/01_create_audit_table.sql` |
| Documentación de endpoints y contratos JSON | `docs/01-documentacion-endpoints.md` |
| Scripts SQL de instalación y reversión | `sql/` y `sql/rollback/` |
| Casos de prueba y evidencias | `tests/Usuarios.Api.Tests/`, `docs/04-plan-pruebas-aceptacion.md`, `postman/Usuarios-Api.postman_collection.json` |
| Instructivo de despliegue y configuración en IIS | `docs/02-instructivo-despliegue-iis.md` |
| Procedimiento de rotación de API Key y recuperación ante fallos | `docs/03-rotacion-apikey-recuperacion.md` |

## Antes de ir a producción, leer primero

**`docs/00-notas-y-supuestos.md`** — documenta qué se confirmó con el
usuario (nombres de tabla `usuario`/`sesion`) y qué quedó como suposición
razonable (columnas, tipos de dato, relación). El README (Sección 5.4) exige
validar esto contra el esquema real antes de ejecutar los scripts SQL en
producción.

## Limitación de este entregable

Este código se generó y revisó manualmente, pero **no se compiló ni se
ejecutó** en este entorno: la generación ocurrió en macOS, y el proyecto
requiere .NET Framework 4.8 + MSBuild/Visual Studio + IIS en Windows para
compilarse y ejecutarse, que no están disponibles aquí. Antes de considerar
esto "listo para producción":

1. Abrir `src/Usuarios.Api/Usuarios.Api.sln` en Visual Studio (Windows),
   restaurar paquetes NuGet y compilar.
2. Corregir cualquier error de compilación que surja (nombres de ensamblado,
   versiones exactas de paquete disponibles en el feed de NuGet de la
   organización, etc.).
3. Ejecutar la suite de pruebas (`tests/Usuarios.Api.Tests/`).
4. Seguir `docs/02-instructivo-despliegue-iis.md` en un ambiente no
   productivo antes de promover a producción (Sección 9, paso 9 del README).
