# API de Gestión de Sesiones y Estado de Usuarios

## 1. Descripción general

Este proyecto implementa una API REST para administrar el cierre de sesiones, el bloqueo y el desbloqueo de usuarios, mediante la ejecución de procedimientos almacenados en SQL Server 2022.

La solución está diseñada para integrarse con la infraestructura existente, sin incorporar nuevas plataformas de ejecución ni requerir la migración de las aplicaciones actuales.

### Objetivos

* Cerrar las sesiones activas de un usuario y bloquearlo.
* Desbloquear un usuario previamente inactivo.
* Centralizar las operaciones mediante procedimientos almacenados.
* Restringir el consumo de la API mediante API Key.
* Registrar cada solicitud, incluyendo llamadas exitosas, fallidas y no autorizadas.
* Garantizar la trazabilidad de las operaciones para auditoría y soporte.

## 2. Stack tecnológico

| Componente                    | Tecnología                        |
| ----------------------------- | --------------------------------- |
| Sistema operativo             | Windows Server existente          |
| Servidor web                  | IIS 10                            |
| Framework                     | .NET Framework 4.8                |
| Tecnología API                | ASP.NET Web API 2                 |
| Lenguaje                      | C#                                |
| Base de datos                 | Microsoft SQL Server 2022         |
| Acceso a datos                | ADO.NET                           |
| Autenticación de consumidores | API Key                           |
| Protocolo de comunicación     | HTTP                             |
| Persistencia de auditoría     | Registro estructurado de llamadas |

La solución debe ser compatible con las aplicaciones actuales alojadas en IIS, sin actualizar el framework de las aplicaciones existentes.

## 3. Arquitectura lógica

La API actuará como capa de integración entre los sistemas consumidores y la base de datos.

```text
Sistema consumidor
       |
       | HTTP + API Key
       v
     IIS 10
       |
       v
ASP.NET Web API 2
.NET Framework 4.8
       |
       +---- Validación de API Key
       |
       +---- Registro de auditoría
       |
       +---- Validación de solicitud
       |
       v
  ADO.NET / SQL Client
       |
       v
 SQL Server 2022
       |
       +---- dbo.usp_CerrarSesionYBloquearUsuario
       |
       +---- dbo.usp_DesbloquearUsuario
```

La API no debe actualizar directamente las tablas de negocio. Todas las modificaciones se realizarán mediante los procedimientos almacenados autorizados.

## 4. Endpoints de la API

La API expondrá dos operaciones principales.

### 4.1. Cerrar sesión y bloquear usuario

**Endpoint**

`POST /api/v1/usuarios/cerrar-sesion-bloquear`

**Propósito**

Cerrar las sesiones del usuario e impedir que continúe operando mediante el cambio de su estado a inactivo.

**Reglas de negocio**

1. Identificar al usuario mediante el identificador definido por el sistema.
2. Cambiar a `INACTIVA` el estado de las sesiones que correspondan al usuario y que deban invalidarse.
3. Cambiar el estado del usuario a `I`, que representa inactivo.
4. Ejecutar los cambios dentro de una transacción para evitar actualizaciones parciales.
5. Registrar el resultado de la operación.

**Solicitud de ejemplo**

```http
POST /api/v1/usuarios/cerrar-sesion-bloquear HTTP/1.1
Content-Type: application/json
X-API-Key: <API_KEY>
```

```json
{
  "usuario": "<IDENTIFICADOR_USUARIO>",
  "motivo": "Bloqueo solicitado por sistema autorizado"
}
```

El campo `motivo` es recomendado para facilitar la auditoría. Su obligatoriedad deberá definirse según las reglas del negocio.

**Resultado exitoso**

Código HTTP: `200 OK`

```json
{
  "codigo": "OK",
  "mensaje": "Sesiones cerradas y usuario bloqueado correctamente",
  "operacion": "CERRAR_SESION_BLOQUEAR",
  "correlationId": "<IDENTIFICADOR_CORRELACION>"
}
```

La respuesta no debe exponer datos internos de SQL Server ni información sensible del usuario.

### 4.2. Desbloquear usuario

**Endpoint**

`POST /api/v1/usuarios/desbloquear`

**Propósito**

Restablecer el estado de un usuario a vigente.

**Reglas de negocio**

1. Identificar al usuario.
2. Actualizar el estado del usuario a `V`, que representa vigente.
3. No modificar la tabla `sesion`.
4. Registrar el resultado de la operación.

**Solicitud de ejemplo**

```http
POST /api/v1/usuarios/desbloquear HTTP/1.1
Content-Type: application/json
X-API-Key: <API_KEY>
```

```json
{
  "usuario": "<IDENTIFICADOR_USUARIO>",
  "motivo": "Desbloqueo solicitado por sistema autorizado"
}
```

**Resultado exitoso**

Código HTTP: `200 OK`

```json
{
  "codigo": "OK",
  "mensaje": "Usuario desbloqueado correctamente",
  "operacion": "DESBLOQUEAR_USUARIO",
  "correlationId": "<IDENTIFICADOR_CORRELACION>"
}
```

**Importante:** desbloquear al usuario no debe reactivar automáticamente las sesiones que se cerraron previamente. El inicio de una nueva sesión debe seguir el proceso normal de autenticación de la aplicación.

## 5. Reglas de negocio y base de datos

### 5.1. Estados permitidos

| Entidad | Estado     | Significado                 |
| ------- | ---------- | --------------------------- |
| Sesión  | `INACTIVA` | Sesión cerrada o invalidada |
| Usuario | `I`        | Inactivo                    |
| Usuario | `V`        | Vigente                     |

Los valores deben respetar los tipos de datos y las restricciones existentes en SQL Server.

### 5.2. Procedimiento almacenado para cerrar sesión y bloquear

Nombre propuesto:

`dbo.usp_CerrarSesionYBloquearUsuario`

Responsabilidades:

* Recibir el identificador del usuario.
* Verificar su existencia.
* Invalidar las sesiones correspondientes.
* Cambiar el estado del usuario a `I`.
* Ejecutar ambas modificaciones dentro de una misma transacción.
* Revertir los cambios ante un error.
* Retornar un resultado controlado que permita distinguir entre éxito, usuario inexistente y error de ejecución.

La operación deberá contemplar usuarios sin sesiones activas y solicitudes repetidas, según las reglas de negocio acordadas.

### 5.3. Procedimiento almacenado para desbloquear

Nombre propuesto:

`dbo.usp_DesbloquearUsuario`

Responsabilidades:

* Recibir el identificador del usuario.
* Verificar su existencia.
* Actualizar exclusivamente el estado del usuario a `V`.
* Retornar un resultado controlado.
* Informar si el usuario no existe o si ocurrió un error.

Este procedimiento no deberá actualizar la tabla `sesion`.

### 5.4. Contrato de los procedimientos

Los nombres anteriores son propuestas y deben ajustarse a los estándares de la base de datos.

Antes de implementarlos es necesario confirmar:

* La clave primaria y el identificador funcional de la tabla `usuario`.
* La relación entre las tablas `usuario` y `sesion`.
* La columna que identifica al usuario en la tabla `sesion`.
* Los nombres y tipos de datos de las columnas de estado.
* Si existen sesiones de diferentes aplicaciones o canales que deban tratarse de manera distinta.

No se deben asumir nombres de columnas ni relaciones que no hayan sido validados en el esquema real.

## 6. Seguridad de la API

### 6.1. API Key

Todas las solicitudes deberán incluir el encabezado:

`X-API-Key: <API_KEY>`

La API deberá validar la clave antes de ejecutar cualquier operación de negocio.

Requisitos:

* Rechazar solicitudes sin clave.
* Rechazar claves incorrectas.
* No aceptar la clave como parámetro de URL.
* No registrar el valor de la clave en logs.
* Permitir su rotación controlada.
* Mantener las claves fuera del código fuente y del repositorio.
* Comparar las claves de manera segura.
* Considerar claves independientes por sistema consumidor.

Respuestas recomendadas:

| Condición                                                 |                        HTTP |
| --------------------------------------------------------- | --------------------------: |
| Clave ausente o inválida                                  |          `401 Unauthorized` |
| Consumidor autenticado sin autorización para la operación |             `403 Forbidden` |
| Solicitud inválida                                        |           `400 Bad Request` |
| Usuario inexistente                                       |             `404 Not Found` |
| Conflicto con el estado actual del usuario                |   `409 Conflict`, si aplica |
| Error interno                                             | `500 Internal Server Error` |

La API Key identifica al consumidor, pero no garantiza por sí sola que este tenga autorización para bloquear cualquier usuario. Cuando aplique, deben incorporarse permisos por consumidor, controles de origen y restricciones de red.


### 6.2. Conexión a SQL Server

La cadena de conexión debe mantenerse en la configuración protegida del servidor.

Ejemplo ilustrativo:

```xml
<connectionStrings>
  <add name="UsuariosDb"
       connectionString="Data Source=SERVIDOR_SQL;
                         Initial Catalog=BASE_DATOS;
                         User ID=USUARIO_API;
                         Password=SECRETO_PROTEGIDO;
                         Encrypt=True;
                         TrustServerCertificate=False;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

Los valores son marcadores de posición. La cadena definitiva debe ajustarse a la configuración de SQL Server y a la política de certificados de la organización.

Controles requeridos:

* Usar un usuario SQL exclusivo para la API, si ese es el mecanismo de autenticación aprobado.
* Otorgar únicamente permisos `EXECUTE` sobre los procedimientos almacenados autorizados.
* Evitar permisos directos de actualización sobre las tablas de negocio.
* Proteger la configuración mediante los mecanismos de cifrado disponibles para IIS y Windows.
* No incluir contraseñas en archivos de configuración versionados.
* Validar los certificados del servidor SQL.
* Mantener las credenciales fuera de los mensajes de error y de auditoría.

## 7. Registro y auditoría

Cada llamada deberá generar un registro, independientemente de si la solicitud fue exitosa o falló.

### 7.1. Campos mínimos de auditoría

| Campo             | Descripción                                                                       |
| ----------------- | --------------------------------------------------------------------------------- |
| Fecha y hora      | Momento de recepción de la solicitud, con zona horaria definida                   |
| Correlation ID    | Identificador único para rastrear la operación                                    |
| Operación         | Cerrar y bloquear, o desbloquear                                                  |
| Consumidor        | Identificador lógico del sistema que llama                                        |
| Usuario objetivo  | Identificador del usuario afectado, sujeto a las políticas de protección de datos |
| Resultado         | Éxito o fallo                                                                     |
| Código HTTP       | Código devuelto al consumidor                                                     |
| Duración          | Tiempo total de procesamiento                                                     |
| Resultado SQL     | Código controlado retornado por el procedimiento                                  |
| Detalle del error | Descripción técnica depurada, sin secretos ni información sensible                |

### 7.2. Eventos que deben registrarse

* Solicitud recibida.
* API Key ausente o inválida.
* Error de validación.
* Ejecución del procedimiento almacenado.
* Operación exitosa.
* Usuario inexistente o estado incompatible.
* Timeout o error de SQL Server.
* Error inesperado de la API.

### 7.3. Requisitos del registro

* No almacenar API Keys, contraseñas ni cadenas de conexión.
* No registrar innecesariamente cuerpos completos de solicitudes.
* Evitar que un consumidor pueda modificar o borrar sus propios registros.
* Establecer políticas de retención, acceso y consulta.
* Centralizar los logs cuando la plataforma corporativa lo permita.

Los logs de IIS permiten registrar solicitudes HTTP, pero deben complementarse con auditoría de aplicación para identificar el resultado de la operación de negocio.

La auditoría de la operación debe diseñarse de modo que un error al registrar el resultado no produzca una respuesta engañosa. Para operaciones críticas puede requerirse una bitácora transaccional en SQL Server o un mecanismo de auditoría independiente y confiable.

## 8. Validaciones técnicas

La implementación deberá incorporar:

* Validación de campos obligatorios y longitudes.
* Validación del formato del identificador del usuario.
* Uso de parámetros SQL, sin concatenación de cadenas.
* Ejecución de procedimientos mediante `CommandType.StoredProcedure`.
* Timeouts configurables para la conexión y los comandos SQL.
* Manejo centralizado de excepciones.
* Respuestas consistentes y sin trazas internas.
* Identificador de correlación para facilitar el soporte.
* Límites de tamaño de solicitud y protección contra llamadas excesivas.
* Manejo de concurrencia y solicitudes repetidas.
* Registro de duración y errores para observabilidad.

Los procedimientos deben ejecutarse con transacciones cuando una operación implique varias modificaciones que deban considerarse una sola unidad de negocio.

## 9. Despliegue en IIS 10

La API se desplegará como una aplicación independiente dentro del IIS existente.

Actividades previstas:

1. Crear la solución en Visual Studio usando ASP.NET Web API 2 y .NET Framework 4.8.
2. Configurar las rutas de la API y los controladores.
3. Implementar la validación de API Key y la auditoría.
4. Configurar el acceso a SQL Server 2022.
5. Crear y validar los procedimientos almacenados en un ambiente no productivo.
6. Publicar la aplicación en un directorio independiente.
7. Configurar la aplicación en IIS y asignarla a un Application Pool apropiado.
8. Ejecutar pruebas funcionales, negativas, de concurrencia y de seguridad.
9. Promover a producción mediante el proceso de cambios establecido por la organización.

Se debe confirmar que el servidor tenga instalados los componentes necesarios de ASP.NET para .NET Framework 4.8 y que la configuración del Application Pool sea compatible.

La nueva aplicación no debe alterar las configuraciones ni las rutas de las aplicaciones existentes.

## 10. Pruebas de aceptación

| Caso                                   | Resultado esperado                                |
| -------------------------------------- | ------------------------------------------------- |
| Cerrar y bloquear un usuario existente | Sesiones inactivas y usuario en estado `I`        |
| Desbloquear un usuario existente       | Usuario en estado `V`                             |
| Desbloquear un usuario                 | Las sesiones no se modifican                      |
| Solicitud sin API Key                  | HTTP `401`; operación no ejecutada                |
| Solicitud con API Key inválida         | HTTP `401`; operación no ejecutada                |
| Identificador de usuario inválido      | HTTP `400`                                        |
| Usuario inexistente                    | Resultado controlado, sin modificaciones          |
| Error al actualizar las sesiones       | Reversión de la transacción de bloqueo            |
| Error al actualizar el usuario         | Reversión de la transacción de bloqueo            |
| SQL Server no disponible               | Error controlado y registro de auditoría          |
| Solicitudes simultáneas                | Estado final consistente                          |
| Solicitud repetida                     | Resultado determinista según las reglas acordadas |
| Cualquier llamada recibida             | Registro de auditoría correspondiente             |

Las pruebas deben confirmar que no quedan sesiones activas después de una operación exitosa de bloqueo, dentro del alcance funcional definido.

Si las aplicaciones mantienen sesiones en memoria, tokens o sesiones en otros componentes, será necesario verificar que cambiar el estado en SQL Server sea suficiente para impedir que continúen utilizándose.

## 11. Entregables

* Código fuente de la API REST.
* Solución de Visual Studio compatible con .NET Framework 4.8.
* Dos procedimientos almacenados documentados.
* Configuración de conexión a SQL Server.
* Mecanismo de validación de API Key.
* Implementación de auditoría.
* Documentación de endpoints y contratos JSON.
* Scripts SQL de instalación y reversión.
* Casos de prueba y evidencias.
* Instructivo de despliegue y configuración en IIS.
* Procedimiento de rotación de API Key y recuperación ante fallos.

## 12. Criterios de finalización

La solución se considerará terminada cuando:

2. Las operaciones se ejecuten exclusivamente mediante los procedimientos almacenados autorizados.
3. El bloqueo actualice las sesiones y el usuario de forma atómica.
4. El desbloqueo modifique únicamente el estado del usuario.
5. Las solicitudes no autorizadas no puedan ejecutar operaciones de negocio.
6. Todas las llamadas generen registros de auditoría trazables.
7. Las credenciales y las claves estén protegidas.
8. Las pruebas funcionales, de seguridad y de manejo de errores sean satisfactorias.
9. La publicación no afecte las aplicaciones existentes en IIS.
10. Existan instrucciones verificables de despliegue, operación y reversión.

---

## Consideraciones pendientes antes de construir

Este README define la arquitectura objetivo. Para implementar los scripts SQL y el código compilable se deben confirmar el esquema real de las tablas, las claves de relación y los mecanismos de sesión utilizados por las aplicaciones consumidoras.

La API Key constituye un control inicial de acceso; para un entorno bancario también deben evaluarse la identidad del consumidor, los permisos por operación, la restricción de red, la gestión centralizada de secretos y la trazabilidad de los cambios de estado.
