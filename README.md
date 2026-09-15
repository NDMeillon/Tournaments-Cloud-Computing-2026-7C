# Tournaments_V1

## Tech Stack

* **Runtime:** .NET 9 / 10+
* **Framework:** ASP.NET Core Minimal APIs
* **Validation:** FluentValidation
* **Testing:** xUnit, `Microsoft.AspNetCore.Mvc.Testing`, FluentAssertions

---
## Solution Structure

```text
TournamentServices/
├── TournamentServices.sln
├── src/
│   └── TournamentServices.Api/
│       ├── Dtos/                     # Request and response contract records
│       │   ├── GroupDtos.cs
|       |   ├── MatchDtos.cs
│       │   ├── TeamDtos.cs
│       │   └── TournamentDtos.cs
│       ├── Extensions/               # Endpoint filters and pipeline extensions
│       │   └── ValidationFilter.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Routes/                   # Minimal API route definitions and handlers
│       │   └── TournamentRoutes.cs
│       ├── Validators/               # FluentValidation request validators
│       ├── appsettings.json
│       ├── Program.cs
│       ├── TournamentServices.Api.csproj
│       └── TournamentServices.Api.http
└── tests/
    └── TournamentServices.Api.Tests/
        ├── Routes/                   # Integration tests using WebApplicationFactory
        |   ├── TeamRoutesTests.cs
        │   └── TournamentRoutesTests.cs
        ├── Validators/               # Unit tests for FluentValidation rules
        │   └── CreateTournamentValidatorTests.cs
        └── TournamentServices.Api.Tests.csproj
```
## Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) (version 9.0 or later)
* Visual Studio Code (or preferred editor)

### Run the API

```bash
# From the project root
dotnet build
dotnet run --project src/TournamentServices.Api
```

The API will start listening at: `http://localhost:8080`
### Run tests.
```bash
# Run unit and integration test suites
dotnet test
```

### API Specification
All request and response bodies use `application/json.` All identifier parameters (`id`, `groupId`, etc.) must match the pattern `^[A-Za-z0-9\-]+$`.
| Method | Route | Description | Status Codes |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/tournaments` | Creates a new tournament | `201 Created`, `400 Bad Request` |
| `GET` | `/api/v1/tournaments/{id}` | Retrieves tournament details by ID | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `POST` | `/api/v1/tournaments/{id}/groups` | Assigns groups to an existing tournament | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `POST` | `/api/v1/tournaments/{id}/groups/{groupId}/teams` | Assigns teams to a tournament group | `200 OK`, `400 Bad Request`, `404 Not Found` |

---

## Next Steps (Roadmap)

- [ ] Scaffold `TournamentServices.Domain` class library with core domain entities (`Tournament`, `Team`, `Group`, `Match`, `Score`).
- [ ] Scaffold `TournamentServices.Delegates` to decouple routing from business orchestration (`ITournamentDelegate`, `TournamentDelegate`, etc.).
- [ ] Implement `TournamentServices.Repositories` for data access and persistence (`ITournamentRepository`, etc.).
- [ ] Add Match and Scoring endpoints and contracts (`MatchRoutes.cs`, `MatchDtos.cs`).
- [ ] Register Delegate and Repository implementations in `Program.cs` to replace route stubs with actual logic.
