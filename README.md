# Turnos API

API REST para gestionar **profesionales, servicios, horarios de atención y reserva de turnos**. Es el resultado de las 6 etapas del proyecto de la materia: persistencia, autenticación, administración, reservas, calidad y entrega.

No tiene frontend: la forma de probarla es **Swagger** (está activo también en producción).

---

## Stack

| Capa | Tecnología |
|---|---|
| API | ASP.NET Core Web API (.NET 10) |
| Persistencia | EF Core + Npgsql (PostgreSQL en Neon) |
| Seguridad | JWT con roles `Cliente` y `Admin`, contraseñas con BCrypt |
| Validación | FluentValidation (filtro global) |
| Errores | Middleware global con `ProblemDetails` (RFC 7807) |
| Tests | xUnit + Moq (21 tests unitarios) |
| Documentación | Swagger / OpenAPI |
| Entrega | Docker multi-stage, Render, GitHub Actions |

## Estructura

```
Turnos.sln
├── src/
│   ├── Turnos.Domain/          entidades y enums (sin dependencias)
│   ├── Turnos.Application/     DTOs, servicios, interfaces de repositorio, validadores
│   ├── Turnos.Infrastructure/  DbContext, repositorios, migraciones, JWT, BCrypt
│   └── Turnos.API/             controladores, filtros, middleware, Program.cs
└── tests/
    └── Turnos.Tests/           tests unitarios de TurnoService y validadores
```

La dependencia va siempre hacia adentro: `API → Application → Domain`, y `Infrastructure` implementa las interfaces que define `Application`.

---

## Puesta en marcha local

### Requisitos

- .NET 10 SDK
- Una base PostgreSQL (en este proyecto usamos Neon)
- `dotnet-ef` como herramienta local (`dotnet tool restore`)

### 1. Configurar secretos

Los secretos **no van al repo**. Se guardan con user-secrets (o como variables de entorno):

```powershell
dotnet user-secrets set --project src/Turnos.API "ConnectionStrings:DefaultConnection" "Host=TU_HOST;Port=5432;Database=neondb;Username=TU_USUARIO;Password=TU_PASSWORD;SSL Mode=Require;Channel Binding=Require;"
dotnet user-secrets set --project src/Turnos.API "Jwt:Key" "una-clave-larga-y-aleatoria"
```

`Jwt:Issuer`, `Jwt:Audience` y `Jwt:ExpiracionMinutos` ya tienen valores por defecto en `appsettings.json` (`TurnosAPI`, `TurnosClient`, `60`).

> Ojo: el formato URI de connection string (`postgres://...`) no funciona bien con Npgsql 10. Usá el formato `Host=...;Password=...;` como arriba.

### 2. Aplicar migraciones

```powershell
dotnet tool restore
dotnet ef database update --project src/Turnos.Infrastructure --startup-project src/Turnos.API
```

La migración de turnos crea la extensión `btree_gist` y la restricción de exclusión que impide dos turnos solapados para el mismo profesional.

### 3. Correr la API

```powershell
dotnet run --project src/Turnos.API --launch-profile http
```

- Swagger: `http://localhost:5194/swagger`
- Health check: `http://localhost:5194/health`

Si el puerto 5194 está ocupado, Kestrel puede elegir otro → **verificá el puerto en la consola** antes de probar.

---

## Autenticación

1. `POST /auth/registro` crea un usuario con rol `Cliente`.
2. `POST /auth/login` devuelve `{ token, expires }`.
3. En Swagger, tocá **Authorize** y pegá el token (sin el prefijo `Bearer`).
4. `GET /auth/me` devuelve los datos del token.

Para hacer de administrador hay que promover al usuario en la base (no hay endpoint para eso, es intencional):

```sql
UPDATE "Usuarios" SET "Rol" = 1 WHERE "Email" = 'tu@email.com';
```

Los enums se serializan como números: `RolUsuario` → `0=Cliente, 1=Admin`; `TurnoEstado` → `0=Pendiente, 1=Confirmado, 2=Cancelado, 3=Completado`.

---

## Endpoints

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `GET` | `/health` | anónimo | Estado del servicio |
| `POST` | `/auth/registro` | anónimo | Registrar cliente |
| `POST` | `/auth/login` | anónimo | Obtener JWT |
| `GET` | `/auth/me` | autenticado | Datos del token |
| `GET` | `/profesionales` | autenticado | Listar profesionales |
| `GET` | `/profesionales/{id}` | autenticado | Detalle de un profesional |
| `GET` | `/profesionales/{id}/disponibilidad?fecha=AAAA-MM-DD&servicioId=1` | autenticado | Franjas libres |
| `POST` | `/profesionales` | Admin | Crear profesional |
| `PUT` / `DELETE` | `/profesionales/{id}` | Admin | Editar / borrar |
| `GET` | `/servicios` | autenticado | Listar servicios |
| `GET` | `/servicios/{id}` | autenticado | Detalle de servicio |
| `POST` | `/servicios` | Admin | Crear servicio |
| `PUT` / `DELETE` | `/servicios/{id}` | Admin | Editar / borrar |
| `GET` | `/horarios/profesional/{id}` | autenticado | Horarios de un profesional |
| `POST` | `/horarios` | Admin | Crear horario |
| `PUT` / `DELETE` | `/horarios/{id}` | Admin | Editar / borrar |
| `GET` | `/turnos/mios` | Cliente | Mis turnos |
| `GET` | `/turnos?profesionalId=&fecha=&estado=` | Admin | Turnos con filtros |
| `POST` | `/turnos` | Cliente | Reservar turno |
| `PUT` | `/turnos/{id}/reprogramar` | Cliente | Reprogramar |
| `POST` | `/turnos/{id}/cancelar` | Cliente | Cancelar |
| `POST` | `/turnos/{id}/confirmar` | Admin | Confirmar |
| `POST` | `/turnos/{id}/completar` | Admin | Completar |

---

## Reglas de negocio

- **Ciclo de estados:** la reserva nace `Pendiente`; el Admin la pasa a `Confirmado` y después a `Completado`. El cliente puede **reprogramar o cancelar** solo mientras está `Pendiente` o `Confirmado`.
- **Anticipo de cancelación:** cada servicio define `HorasAnticipoCancelacion` (default `2` h; `Consulta medica` usa `24` h).
- **Disponibilidad:** se generan franjas dentro del horario de atención del día. Si se pasa `servicioId`, usa su duración; si no, usa la menor duración entre los servicios del profesional (fallback 30 min).
- **Días:** `Lunes=1 … Domingo=7`.
- **Fechas:** todo en UTC. Un `DateOnly` se interpreta como fecha UTC.
- **Concurrencia:** la reserva usa una transacción con `pg_advisory_xact_lock(profesionalId)` más una *exclusion constraint* en PostgreSQL. Si dos request compiten, el perdedor recibe `409`.
- **Sin expresiones en la vista:** ya que no hay frontend, toda la lógica vive en los servicios de `Application`.

---

## Validaciones y errores

Cada request tiene un validador de FluentValidation. Un filtro global los ejecuta **antes** del controlador, así que los controladores solo llaman al servicio.

Todas las respuestas de error usan `application/problem+json`:

| Situación | HTTP |
|---|---|
| Campo inválido | `400` + objeto `errors` por campo |
| Regla de negocio / argumento inválido | `400` |
| Recurso no encontrado | `404` |
| Login incorrecto | `401` |
| Turno de otro usuario | `403` |
| Dos turnos solapados (constraint) | `409` |
| Error no previsto | `500` |

Ejemplo:

```json
{
  "status": 400,
  "title": "Validación",
  "detail": "Uno o más campos son inválidos.",
  "errors": { "Email": ["'Email' no es una dirección de correo electrónico válida."] }
}
```

---

## Tests

```powershell
dotnet test Turnos.sln
```

Los 21 tests son unitarios: usan **Moq** para simular los repositorios, así que **no necesitan base de datos**. Cubren superposición, horario de atención, cálculo de franjas, anticipo de 2 h vs 24 h, ciclo de estados, reprogramación, propiedad del turno y todos los validadores.

Los tests de integración (`WebApplicationFactory` contra otra base) quedaron como extra opcional del PDF y no están incluidos.

---

## Docker

```powershell
docker build -t turnos-api .
docker run -p 10000:10000 `
  -e ConnectionStrings__DefaultConnection="Host=...;Password=...;" `
  -e Jwt__Key="una-clave-larga-y-aleatoria" `
  turnos-api
```

El Dockerfile es **multi-stage**: compila con el SDK y publica solo el output en la imagen de runtime (más chica). Corre como usuario no root y usa `.dockerignore` para no copiar `bin`, `obj`, `.git` ni secretos.

La app escucha en `$PORT` (default `10000`), que es lo que Render setea automáticamente.

---

## Deploy en Render

El repo incluye `render.yaml` (blueprint). Render **no** ejecuta migraciones automáticamente: aplicar `dotnet ef database update` contra la base de producción antes del primer deploy.

1. Subir el repo a GitHub (el remote ya apunta a `turnos-api-dotnet`).
2. En Render: **New + → Blueprint** y conectar el repositorio. Detecta `render.yaml` solo.
3. Completar los dos secretos que Render va a pedir (`sync: false`):
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Key`
4. El resto ya viene cargado: imagen Docker desde `./Dockerfile`, región `sa-east-1` (cerca de Neon), plan free y health check en `/health`.
5. Swagger queda en `https://<tu-servicio>.onrender.com/swagger`.

Si preferís configurar a mano: **New + → Web Service**, Language `Docker`, Dockerfile Path `./Dockerfile`, Health Check Path `/health`, y cargá las mismas variables de entorno.

> En el plan free el servicio se duerme cuando no recibe tráfico; el primer request después de un rato puede tardar unos segundos.

### Deploy actual y verificación

La API está desplegada y funcionando en:

**https://turnos-api-dotnet.onrender.com/swagger/index.html**

Chequeos realizados contra el deploy:

| Prueba | Resultado |
|---|---|
| `GET /health` | `200` |
| `GET /swagger/index.html` y `/swagger/v1/swagger.json` | `200` (18 rutas documentadas) |
| `POST /auth/login` | `200`, devuelve token JWT |
| `GET /profesionales`, `/servicios`, `/turnos/mios` con token | `200` con datos de Neon |
| Endpoint protegido sin token | `401` |
| Request inválido | `400` `problem+json` con `errors` |
| GitHub Actions (tests + `docker build`) | ambos jobs en verde |

> **Trampa con la connection string:** en Render el valor va **sin comillas** y el nombre con doble guion bajo (`ConnectionStrings__DefaultConnection`). Las comillas que se ven en `secrets.json` son sintaxis del JSON, no parte del valor: si se copian, Npgsql falla con `Format of the initialization string does not conform to specification...`. Tampoco sirve el formato URI `postgres://...`: Npgsql 10 necesita `Host=...;Password=...;`.

### Variables de entorno

| Variable | Origen | Descripción |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | secreto | Connection string de Neon |
| `Jwt__Key` | secreto | Clave de firma del token |
| `Jwt__Issuer` | `render.yaml` | `TurnosAPI` |
| `Jwt__Audience` | `render.yaml` | `TurnosClient` |
| `Jwt__ExpiracionMinutos` | `render.yaml` | `60` |
| `PORT` | Render | `10000` por defecto |

---

## CI

GitHub Actions (`.github/workflows/ci.yml`) corre en cada push y PR a `main`:

1. Restaurar, compilar en `Release` y correr los tests.
2. Construir la imagen Docker para validar el `Dockerfile`.

Los tests no necesitan secretos ni base de datos, así que el workflow es autocontenido.

---

## Estado

- [x] Etapa 1 — proyecto, EF Core y Neon
- [x] Etapa 2 — registro, login, JWT y roles
- [x] Etapa 3 — administración de profesionales, servicios y horarios
- [x] Etapa 4 — turnos, disponibilidad y concurrencia
- [x] Etapa 5 — tests, validaciones y manejo de errores
- [x] Etapa 6 — Swagger, README, CI, Docker, `/health`, deploy en Render y CI en verde

El commit `a82ebc5` está pusheado a `main` y el deploy en Render está funcionando. Los 21 tests unitarios y el `docker build` pasan en GitHub Actions.

---

## Licencia

MIT — ver [LICENSE](LICENSE).
