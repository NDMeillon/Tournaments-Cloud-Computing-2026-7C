# Tournament Services

API REST de torneos (equipos, grupos y partidos) sobre ASP.NET Core Minimal APIs y PostgreSQL.

## Stack

* **Runtime:** .NET 9
* **Framework:** ASP.NET Core Minimal APIs
* **Persistencia:** EF Core + PostgreSQL 16 (contenedor Docker)
* **Validación:** FluentValidation
* **Pruebas:** xUnit, `WebApplicationFactory` (SQLite en archivo temporal) y pruebas de dominio
* **CI:** Jenkins (`Jenkinsfile` en la raíz)

## Estructura

```text
TournamentServices/
├── src/
│   ├── TournamentServices.Domain/          # Entidades y reglas de marcador
│   ├── TournamentServices.Infrastructure/  # DbContext, migración, PostgreSQL
│   └── TournamentServices.Api/             # Rutas, DTOs, delegados, repositorios
└── tests/
    ├── TournamentServices.Api.Tests/
    └── TournamentServices.Domain.Tests/
```

## Cómo correrlo

Requisitos: .NET SDK 9, Docker.

```bash
docker compose up -d db
cd TournamentServices
dotnet tool restore
dotnet ef database update --project src/TournamentServices.Infrastructure --startup-project src/TournamentServices.Api
dotnet run --project src/TournamentServices.Api
```

La API queda en `http://localhost:8080`. La raíz muestra el estado y `/swagger` permite probar los endpoints. `GET /health` responde `Services running`.

En este repositorio, cada `git commit` ejecuta `dotnet test` con el hook `scripts/pre-commit`. En GitHub, el workflow `.github/workflows/tests.yml` vuelve a correrlos en cada push.

Al arrancar en local, la API también aplica las migraciones pendientes si PostgreSQL ya está arriba.

```bash
cd TournamentServices
dotnet test
```

Los tests de la API no usan el contenedor: cada clase levanta un SQLite propio. El contenedor es la base de la aplicación y el paso `Migrate` de Jenkins.

## Jenkins

El `Jenkinsfile` hace, en el agente:

1. `docker compose up -d --wait db`
2. `dotnet build`
3. `dotnet ef database update` contra `localhost:5432`
4. `dotnet test`
5. `docker compose down`

El agente necesita Docker, el SDK de .NET 9 y acceso al puerto 5432.

## Contrato

Los cuerpos son `application/json`. Los ids cumplen `^[A-Za-z0-9\-]+$`.

| Método | Ruta | Códigos |
| :--- | :--- | :--- |
| `GET` | `/health` | `200` |
| `GET/POST` | `/teams` | `200`, `201`, `400` |
| `GET/PUT/DELETE` | `/teams/{teamId}` | `200`, `204`, `400`, `404` |
| `GET/POST` | `/tournaments` | `200`, `201`, `400` |
| `GET/PUT/PATCH/DELETE` | `/tournaments/{tournamentId}` | `200`, `204`, `400`, `404` |
| `GET/POST` | `/tournaments/{tournamentId}/groups` | `200`, `201`, `400`, `404`, `422` |
| `GET/PUT/DELETE` | `/tournaments/{tournamentId}/groups/{groupId}` | `200`, `204`, `400`, `404` |
| `PATCH` | `/tournaments/{tournamentId}/groups/{groupId}/teams` | `204`, `400`, `404`, `422` |
| `GET/POST` | `/tournaments/{tournamentId}/matches` | `200`, `201`, `400`, `404`, `422` |
| `GET/DELETE` | `/tournaments/{tournamentId}/matches/{matchId}` | `200`, `204`, `400`, `404` |
| `PATCH` | `/tournaments/{tournamentId}/matches/{matchId}/score` | `200`, `400`, `404` |
