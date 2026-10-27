# Tournament Services — estado y demo

Guía para explicar el proyecto en unos 20 minutos y para repetir la demo en local.
Fuente: `Proyecto_Final_C7.md`, el código de `TournamentServices/` y los pipelines (`Jenkinsfile`, `.github/workflows/tests.yml`).
Revisión: 26 de octubre de 2026.

Para que este archivo se quede en el historial del repositorio hay que incluirlo en un commit. Mientras no se haga commit, existe solo en la copia de trabajo.

## Frase de apertura

El servicio ya administra un torneo de punta a punta: equipos, grupos, partidos y marcador. Lo que queda es alinear la estructura y las pruebas con el enunciado, no volver a construir la API.

## Guion de 20 minutos

| Minuto | Bloque | Qué decir |
| :--- | :--- | :--- |
| 0–2 | El problema | Una API para armar un torneo. Quien la consume no toca la base. |
| 2–6 | El modelo | Equipo, torneo con formato, grupo y partido. El marcador calcula el ganador. |
| 6–11 | Una petición | La ruta valida, el delegado aplica la regla, el repositorio guarda. 400, 404 y 422 separan dato mal formado, recurso inexistente y regla de negocio. |
| 11–15 | Lo que ya corre | Demo corta: torneo, dos equipos, un grupo, un partido, marcador 2-1 con ganador `HOME`. |
| 15–18 | Cómo se verifica | Pruebas con SQLite. PostgreSQL 16 es la base real. Jenkins migra y prueba. GitHub Actions vuelve a correr `dotnet test`. |
| 18–20 | Qué sigue | Cerrar pruebas de partidos, unir las reglas al dominio, y los proyectos de prueba de delegados y repositorios. |

## De qué va

Cuatro recursos:

- **Equipo:** `id` y `name`. El nombre no se repite.
- **Torneo:** nombre y formato (`maxTeamsPerGroup`, `numberOfGroups`, tipo `ROUND_ROBIN` o `NFL`).
- **Grupo:** pertenece a un torneo y recibe equipos hasta el máximo del formato.
- **Partido:** local y visita. Si trae grupo, ambos equipos tienen que estar en ese grupo. Al anotar, el ganador es `HOME`, `VISITOR` o vacío si empatan. Un marcador negativo se rechaza.

Base URL: `http://localhost:8080`. Cuerpos `application/json`. Ids con la forma `^[A-Za-z0-9\-]+$`.

| Código | Cuándo |
| :--- | :--- |
| 200 / 201 | La operación sale bien. El alta trae cabecera `Location` y el recurso creado. |
| 204 | Borrado, o asignación de equipos a un grupo. |
| 400 | Id inválido o JSON que no pasa FluentValidation. |
| 404 | El recurso no existe. |
| 422 | Regla de negocio: nombre de grupo repetido, cupo lleno, el mismo equipo de local y de visita. |

## Cómo está armado

El enunciado dibuja cuatro proyectos (`Api`, `Delegates`, `Repositories`, `Domain`). En el repo hay tres:

| Capa | Dónde está | Qué hace |
| :--- | :--- | :--- |
| Rutas Minimal API | `TournamentServices.Api/Routes` | Reciben HTTP, validan y traducen el resultado a status code. |
| Delegados | `TournamentServices.Api/Delegates` | Reglas: nombre único, cupo, equipos del mismo grupo, marcador. |
| Repositorios EF | `TournamentServices.Api/Repositories` | Traducen DTOs a entidades y guardan. |
| Dominio | `TournamentServices.Domain` | Entidades, formato y marcador. |
| Infraestructura | `TournamentServices.Infrastructure` | DbContext, migración y PostgreSQL. |

Las reglas de grupos y partidos están escritas dos veces. El delegado las aplica sobre DTOs. El dominio tiene los mismos chequeos en `AddGroup`, `AssignTeams` y `Match.Create`, y la API no los llama. Solo `RecordScore` entra en el camino real, al persistir el marcador.

## Qué ya se cumplió

- Los 23 endpoints del contrato: salud, equipos, torneos (`PUT` y `PATCH`), grupos (incluye asignar equipos) y partidos (incluye marcador).
- `GET /health` responde el texto `Services running`. La raíz muestra una página HTML. `/swagger` lista los endpoints.
- FluentValidation y respuestas de error. Escrituras dentro de una transacción (`IUnitOfWork`).
- PostgreSQL 16 con EF Core. Tablas `teams`, `tournaments`, `groups`, `matches` y la puente `group_teams`. Al arrancar contra PostgreSQL, la API aplica migraciones pendientes.
- Borrar un torneo arrastra grupos y partidos. Borrar un equipo lo saca de sus grupos. Si el equipo todavía aparece en un partido, el borrado se rechaza.
- Unas 80 pruebas. Equipos, torneos (incluye un Mundial de 32 equipos en 8 grupos) y grupos están bien cubiertos. Cada clase de pruebas de API usa un SQLite propio.
- `docker-compose.yml` levanta `postgres:16-alpine` en el puerto 5432.
- `Jenkinsfile`: restaura, compila en Release, `dotnet ef database update`, `dotnet test` y `docker compose down`.
- GitHub Actions corre `dotnet test` en cada push y cada pull request.
- `scripts/pre-commit` corre `dotnet test` antes de cada commit.

## Qué falta o qué sigue

1. **Una prueba de dominio no coincide con el modelo.** `Winner_BeforeScore_IsNull` en `tests/TournamentServices.Domain.Tests/MatchTests.cs` asigna `HomeScore` y `VisitorScore`. El marcador vive en `Match.Score`.
2. **Pruebas de partidos incompletas.** Falta lista vacía, ver un partido por id, borrarlo con 204 y rechazar un id inválido. En grupos falta afirmar la lista vacía y que borrar un grupo suelta a sus equipos.
3. **Unir las reglas al dominio** para que el delegado llame a `AddGroup`, `AssignTeams` y `Match.Create` en lugar de repetir la regla.
4. **Proyectos de prueba que el enunciado nombra y no existen:** `TournamentServices.Delegates.Tests` y `TournamentServices.Repositories.Tests`.
5. **Umbral de cobertura en el pipeline.** El enunciado pide `dotnet test --collect:"XPlat Code Coverage"` con mínimos (rutas 100 %, delegados 90 %, repositorios 80 %, dominio 100 % de la lógica). Jenkins y GitHub Actions ejecutan las pruebas sin ese umbral.
6. **`ROUND_ROBIN` y `NFL` se guardan y se pueden cambiar con PATCH.** No cambian el calendario. `CreateWithGroupsAsync` arma grupos A, B, C… y no está publicado en ninguna ruta.
7. **Marco de trabajo mezclado.** El enunciado pide .NET 10 o superior. Api, Domain e Infrastructure apuntan a `net9.0`. El proyecto de pruebas de la API apunta a `net10.0`.

El enunciado deja para después, y conviene decirlo en voz alta: autenticación (401/403), eventos al crear o borrar un torneo, y una prueba de peticiones en paralelo.

## Cierre de la charla

Recorrer la demo de abajo. Enseñar el 422 de un nombre de grupo repetido y el 400 de un id con un carácter fuera del patrón. Cerrar con la lista de pendientes: la API del contrato está, y el trabajo que sigue es unir reglas, pruebas de delegado y repositorio, y el umbral de cobertura.

---

# Cómo correrlo

Requisitos en la máquina: Docker Desktop encendido, y el SDK de .NET que pueda compilar `net9.0` (la API) y `net10.0` (el proyecto `TournamentServices.Api.Tests`, si se va a correr `dotnet test` de toda la solución).

Abrir una terminal en la raíz del repositorio (`Tournaments-Cloud-Computing-2026-7C`).

## 1. Base de datos

```powershell
docker compose up -d db
```

Eso levanta PostgreSQL 16 en `localhost:5432`, base `tournament_db`, usuario `tournament_svc`, contraseña `password`. El puerto 5432 tiene que estar libre.

## 2. API

```powershell
cd TournamentServices
dotnet run --project src/TournamentServices.Api
```

Dejar esa terminal abierta. La API queda en `http://localhost:8080`. Al arrancar, si PostgreSQL ya responde, aplica las migraciones pendientes. No hace falta correr `dotnet ef database update` a mano para la demo.

Comprobar, en otra terminal:

```powershell
curl.exe http://localhost:8080/health
```

Tiene que imprimir `Services running`.

Abrir en el navegador:

- `http://localhost:8080` — página de inicio
- `http://localhost:8080/swagger` — desde aquí se hace la demo

## 3. Pruebas (opcional, antes o después de la demo)

En otra terminal, desde `TournamentServices`:

```powershell
dotnet test
```

Las pruebas de la API no usan el contenedor de PostgreSQL.

---

# Demo en Swagger

Swagger: `http://localhost:8080/swagger`. En cada paso, **Try it out**, pegar el JSON, **Execute**. Anotar los `id` que devuelve el cuerpo: hacen falta en el paso siguiente.

El servicio genera los ids (`team-…`, `trn-…`, `grp-…`, `match-…`). En los ejemplos de abajo, sustituir los placeholders por los ids reales de la respuesta anterior.

## Paso 1 — Crear el torneo

`POST /tournaments`

```json
{
  "name": "Copa Demo",
  "format": {
    "maxTeamsPerGroup": 4,
    "numberOfGroups": 2,
    "type": "ROUND_ROBIN"
  }
}
```

Esperado: **201**. Copiar `id` del cuerpo. Ejemplo de forma: `trn-a1b2c3d4`. Llamarlo `TOURNAMENT_ID`.

## Paso 2 — Crear dos equipos

`POST /teams`, dos veces.

```json
{ "name": "Mexico" }
```

```json
{ "name": "Brasil" }
```

Esperado: **201** en cada una. Copiar cada `id`. Llamarlos `TEAM_MEX` y `TEAM_BRA`.

## Paso 3 — Crear el grupo

`POST /tournaments/{tournamentId}/groups`

Poner `TOURNAMENT_ID` en el parámetro `tournamentId`.

```json
{ "name": "Grupo A" }
```

Esperado: **201**. Copiar `id`. Llamarlo `GROUP_ID`.

## Paso 4 — Meter los equipos en el grupo

`PATCH /tournaments/{tournamentId}/groups/{groupId}/teams`

```json
{
  "teamIds": ["TEAM_MEX", "TEAM_BRA"]
}
```

Esperado: **204** sin cuerpo.

## Paso 5 — Crear el partido

`POST /tournaments/{tournamentId}/matches`

```json
{
  "groupId": "GROUP_ID",
  "homeTeamId": "TEAM_MEX",
  "visitorTeamId": "TEAM_BRA"
}
```

Esperado: **201**. El marcador viene en 0-0, `winner` vacío e `isCompleted` en false. Copiar `id`. Llamarlo `MATCH_ID`.

## Paso 6 — Cargar el marcador

`PATCH /tournaments/{tournamentId}/matches/{matchId}/score`

```json
{
  "homeTeamScore": 2,
  "visitorTeamScore": 1
}
```

Esperado: **200**. En el cuerpo, `winner` es `HOME` e `isCompleted` es true. Mexico es el local, así que gana el local.

## Paso 7 — Ver el torneo completo

`GET /tournaments/{tournamentId}`

Esperado: **200** con el grupo, los dos equipos y el partido ya jugado.

## Dos errores que cierran la explicación

**422, regla de negocio.** `POST /tournaments/{tournamentId}/groups` otra vez con el mismo nombre:

```json
{ "name": "Grupo A" }
```

Esperado: **422**. Ya existe un grupo con ese nombre en el torneo.

**400, id inválido.** `GET /teams/{teamId}` con `teamId` igual a `hola_mundo`. El guion bajo no entra en `^[A-Za-z0-9\-]+$`.

Esperado: **400**.

Opcional: `POST /teams` con `{ "name": "" }` también responde **400** por validación.

---

# La misma demo por terminal (PowerShell)

Con la API ya corriendo, en otra terminal. Cada alta imprime el id para copiarlo al siguiente comando.

```powershell
$base = "http://localhost:8080"

$tournament = Invoke-RestMethod -Method Post -Uri "$base/tournaments" -ContentType "application/json" -Body '{"name":"Copa Demo","format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":"ROUND_ROBIN"}}'
$mex = Invoke-RestMethod -Method Post -Uri "$base/teams" -ContentType "application/json" -Body '{"name":"Mexico"}'
$bra = Invoke-RestMethod -Method Post -Uri "$base/teams" -ContentType "application/json" -Body '{"name":"Brasil"}'
$group = Invoke-RestMethod -Method Post -Uri "$base/tournaments/$($tournament.id)/groups" -ContentType "application/json" -Body '{"name":"Grupo A"}'

Invoke-RestMethod -Method Patch -Uri "$base/tournaments/$($tournament.id)/groups/$($group.id)/teams" -ContentType "application/json" -Body (@{ teamIds = @($mex.id, $bra.id) } | ConvertTo-Json)

$matchBody = @{ groupId = $group.id; homeTeamId = $mex.id; visitorTeamId = $bra.id } | ConvertTo-Json
$match = Invoke-RestMethod -Method Post -Uri "$base/tournaments/$($tournament.id)/matches" -ContentType "application/json" -Body $matchBody

$played = Invoke-RestMethod -Method Patch -Uri "$base/tournaments/$($tournament.id)/matches/$($match.id)/score" -ContentType "application/json" -Body '{"homeTeamScore":2,"visitorTeamScore":1}'
$played | ConvertTo-Json -Depth 6
```

La última impresión tiene que mostrar `winner` igual a `HOME`.

Si se corre el script dos veces seguidas, el segundo `Mexico` o `Brasil` falla porque el nombre de equipo es único. Para repetir la demo, usar otros nombres o borrar los equipos desde Swagger (`DELETE /teams/{teamId}`) si no quedaron en un partido.

## Apagar

En la terminal de la API, `Ctrl+C`. Luego, desde la raíz del repositorio:

```powershell
docker compose down
```

`docker compose down` apaga el contenedor y conserva los datos en el volumen `tournament_pg`. Para vaciar también la base: `docker compose down -v`.
