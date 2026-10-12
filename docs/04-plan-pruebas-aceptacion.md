# Plan y evidencias de pruebas de aceptación

Basado en la matriz de la Sección 10 del README. Para cada caso se indica
cómo se verifica: automatizado (proyecto `Usuarios.Api.Tests`), manual
(Postman/curl contra un ambiente con `sql/00_esquema_prueba_opcional.sql`), o
ambos.

| # | Caso | Resultado esperado | Verificación |
| - | ---- | ------------------- | ------------- |
| 1 | Cerrar y bloquear un usuario existente | Sesiones `INACTIVA` y usuario en `I` | Automatizado: `UsuariosControllerTests.CerrarSesionYBloquear_UsuarioExistente_Retorna200YAuditaExito`. Manual: ejecutar endpoint y luego `SELECT estado FROM sesion WHERE id_usuario = ...` / `SELECT estado FROM usuario WHERE usuario = ...` |
| 2 | Desbloquear un usuario existente | Usuario en `V` | Automatizado: `UsuariosControllerTests.Desbloquear_UsuarioExistente_Retorna200` |
| 3 | Desbloquear un usuario | Las sesiones no se modifican | Automatizado: `Desbloquear_UsuarioExistente_Retorna200` verifica que no se invoque `CerrarSesionYBloquearAsync`. Manual: comparar `sesion` antes/después del `POST /desbloquear` |
| 4 | Solicitud sin API Key | `401`; operación no ejecutada | Automatizado: `ApiKeyAuthHandlerTests.SendAsync_SinEncabezadoApiKey_Retorna401YNoLlamaAlInnerHandler` |
| 5 | Solicitud con API Key inválida | `401`; operación no ejecutada | Automatizado: `ApiKeyAuthHandlerTests.SendAsync_ApiKeyInvalida_Retorna401` |
| 6 | Identificador de usuario inválido | `400` | Manual/Postman: enviar `usuario` vacío o con caracteres no permitidos (ej. `"usuario": "a b"`); cubierto por `ValidateModelStateAttribute` + Data Annotations en `UsuarioOperationRequest` |
| 7 | Usuario inexistente | Resultado controlado, sin modificaciones | Automatizado: `CerrarSesionYBloquear_UsuarioInexistente_Retorna404` |
| 8 | Error al actualizar las sesiones | Reversión de la transacción de bloqueo | Manual en ambiente de prueba: forzar un error (p. ej. constraint temporal en `sesion`) y confirmar que `usuario.estado` tampoco cambió, vía `sql/02_create_usp_CerrarSesionYBloquearUsuario.sql` (TRY/CATCH + ROLLBACK) |
| 9 | Error al actualizar el usuario | Reversión de la transacción de bloqueo | Igual al caso 8, forzando el error en el segundo `UPDATE` |
| 10 | SQL Server no disponible | Error controlado y registro de auditoría | Automatizado: `CerrarSesionYBloquear_SqlServerNoDisponible_Retorna500YAuditaSinExponerDetalleInterno`. Manual: detener el servicio de SQL Server en un ambiente de prueba y repetir el caso 1 |
| 11 | Solicitudes simultáneas | Estado final consistente | Manual: disparar N solicitudes concurrentes de bloqueo sobre el mismo usuario (ej. `ab -n 20 -c 20` o un script paralelo) y verificar un único estado final consistente en `usuario`/`sesion` |
| 12 | Solicitud repetida | Resultado determinista | Manual: invocar bloqueo dos veces seguidas sobre el mismo usuario; ambas responden `200` (ver `docs/00-notas-y-supuestos.md`, punto 4) |
| 13 | Cualquier llamada recibida | Registro de auditoría correspondiente | Manual: tras ejecutar la suite de Postman completa, `SELECT COUNT(*) FROM dbo.ApiAuditoria WHERE CorrelationId IN (...)` debe igualar el número de llamadas realizadas |

## Ejecutar las pruebas automatizadas

```cmd
nuget restore src\Usuarios.Api\Usuarios.Api.sln
msbuild tests\Usuarios.Api.Tests\Usuarios.Api.Tests.csproj /p:Configuration=Release
vstest.console.exe tests\Usuarios.Api.Tests\bin\Release\Usuarios.Api.Tests.dll
```

## Ejecutar las pruebas manuales

1. Levantar un ambiente de pruebas con `sql/00_esquema_prueba_opcional.sql`
   + `sql/01`–`04`.
2. Importar `postman/Usuarios-Api.postman_collection.json` en Postman.
3. Configurar las variables de entorno `baseUrl` y `apiKey` (usar la clave de
   `Secrets\ApiKeys.config` del ambiente de pruebas).
4. Ejecutar la colección completa (Collection Runner) y revisar que cada
   solicitud cumpla el código HTTP esperado.
5. Adjuntar como evidencia: el reporte del Collection Runner y un `SELECT *
   FROM dbo.ApiAuditoria ORDER BY Id DESC` posterior a la corrida.
