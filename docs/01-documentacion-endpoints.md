# Documentación de endpoints — API de Gestión de Sesiones y Estado de Usuarios

Base URL (producción): `https://<host-iis>/usuarios-api`
Todas las rutas están versionadas bajo `/api/v1/usuarios`.

## Autenticación

Todas las solicitudes requieren el encabezado:

```
X-API-Key: <clave asignada al consumidor>
```

- Nunca se acepta como parámetro de query string.
- Una clave inválida o ausente responde `401 Unauthorized` sin ejecutar
  ninguna lógica de negocio.

## Correlación

Toda solicitud puede incluir opcionalmente:

```
X-Correlation-Id: <GUID>
```

Si se omite, o no tiene forma de GUID, la API genera uno nuevo. El valor
usado se refleja siempre en la respuesta (encabezado `X-Correlation-Id`) y en
el cuerpo (`correlationId`), y es la clave para buscar la operación en
`dbo.ApiAuditoria`.

---

## POST /api/v1/usuarios/cerrar-sesion-bloquear

Cierra las sesiones activas del usuario y lo deja en estado inactivo (`I`).

### Solicitud

```http
POST /api/v1/usuarios/cerrar-sesion-bloquear HTTP/1.1
Content-Type: application/json
X-API-Key: <API_KEY>
X-Correlation-Id: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

```json
{
  "usuario": "jperez",
  "motivo": "Bloqueo solicitado por sistema autorizado"
}
```

| Campo   | Tipo   | Obligatorio | Reglas                                            |
| ------- | ------ | ----------- | -------------------------------------------------- |
| usuario | string | Sí          | 1–50 caracteres, `[A-Za-z0-9_.\-@]+`               |
| motivo  | string | No          | Máx. 500 caracteres. Se valida pero no se persiste (ver `docs/00-notas-y-supuestos.md`) |

### Respuestas

**200 OK**
```json
{
  "codigo": "OK",
  "mensaje": "Sesiones cerradas y usuario bloqueado correctamente",
  "operacion": "CERRAR_SESION_BLOQUEAR",
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**400 Bad Request** — el cuerpo no cumple las reglas de validación.
```json
{
  "codigo": "SOLICITUD_INVALIDA",
  "mensaje": "La solicitud no cumple con el formato esperado",
  "correlationId": "...",
  "errores": ["El campo 'usuario' es obligatorio"]
}
```

**401 Unauthorized** — API Key ausente o inválida.
```json
{ "codigo": "NO_AUTORIZADO", "mensaje": "API Key ausente o inválida", "correlationId": "..." }
```

**404 Not Found** — el usuario no existe.
```json
{ "codigo": "USUARIO_NO_ENCONTRADO", "mensaje": "El usuario indicado no existe", "correlationId": "..." }
```

**500 Internal Server Error** — error inesperado o SQL Server no disponible.
```json
{ "codigo": "ERROR_INTERNO", "mensaje": "Ocurrió un error inesperado al procesar la solicitud", "correlationId": "..." }
```

### Efecto en base de datos

Ejecuta `dbo.usp_CerrarSesionYBloquearUsuario` dentro de una transacción:
invalida (`INACTIVA`) las sesiones del usuario y cambia `usuario.estado` a
`I`. Ver `sql/02_create_usp_CerrarSesionYBloquearUsuario.sql`.

---

## POST /api/v1/usuarios/desbloquear

Restablece el estado del usuario a vigente (`V`). **No modifica la tabla
`sesion`**: el usuario deberá iniciar sesión nuevamente por el flujo normal de
autenticación de cada aplicación.

### Solicitud

```http
POST /api/v1/usuarios/desbloquear HTTP/1.1
Content-Type: application/json
X-API-Key: <API_KEY>
```

```json
{
  "usuario": "jperez",
  "motivo": "Desbloqueo solicitado por sistema autorizado"
}
```

Mismas reglas de campo que el endpoint anterior.

### Respuestas

**200 OK**
```json
{
  "codigo": "OK",
  "mensaje": "Usuario desbloqueado correctamente",
  "operacion": "DESBLOQUEAR_USUARIO",
  "correlationId": "..."
}
```

Los códigos `400`, `401`, `404` y `500` tienen la misma forma que en el
endpoint anterior.

### Efecto en base de datos

Ejecuta `dbo.usp_DesbloquearUsuario`: cambia únicamente `usuario.estado` a
`V`. No toca `dbo.sesion`. Ver `sql/03_create_usp_DesbloquearUsuario.sql`.

---

## Tabla de códigos HTTP

| Condición                                                 | HTTP |
| ---------------------------------------------------------- | ---: |
| Éxito                                                       |  200 |
| Solicitud inválida (campos faltantes/formato incorrecto)    |  400 |
| API Key ausente o inválida                                  |  401 |
| Usuario inexistente                                         |  404 |
| Error interno / SQL Server no disponible                    |  500 |

> El README original (Sección 6.1) contempla también `403 Forbidden` para un
> consumidor autenticado pero sin autorización para una operación específica,
> y `409 Conflict` ante un conflicto de estado. Esta implementación no
> distingue permisos por operación dentro de la misma API Key (todo
> consumidor válido puede invocar ambos endpoints) ni trata una solicitud
> repetida como conflicto (ver `docs/00-notas-y-supuestos.md`, punto 4). Si el
> negocio requiere permisos granulares por consumidor o semántica `409`, debe
> ampliarse `Services/ApiKeyProvider.cs` y la lógica de
> `Controllers/UsuariosController.cs` respectivamente.
