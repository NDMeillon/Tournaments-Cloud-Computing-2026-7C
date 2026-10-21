# Tournament Services - REST API Contract (PRD)

## Overview
- **Service**: Tournament Management REST API
- **Language/Framework**: C# .NET 10+ with Minimal APIs
- **Base URL**: `http://localhost:8080` (configurable via `appsettings.json`)
- **Content-Type**: `application/json`
- **ID Format**: Regex `[A-Za-z0-9\-]+`

---

## Architecture

### Layered Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer (Minimal APIs)             │
│  • Endpoint definitions (MapGet, MapPost, MapPut, etc.)     │
│  • Request/Response mapping                                 │
│  • Validation (FluentValidation)                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Delegate Layer                         │
│  • ITournamentDelegate                                      │
│  • ITeamDelegate                                            │
│  • IGroupDelegate                                           │
│  • IMatchDelegate                                           │
│  • Business logic orchestration                             │
│  • Transaction management                                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Repository Layer                         │
│  • ITournamentRepository                                    │
│  • ITeamRepository                                          │
│  • IGroupRepository                                         │
│  • IMatchRepository                                         │
│  • Data access (EF Core / Dapper)                           │
│  • Query building                                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Models                          │
│  • Tournament, Team, Group, Match, Score                    │
│  • Value Objects (TournamentFormat, Score)                  │
│  • Enums (TournamentType, Winner)                           │
└─────────────────────────────────────────────────────────────┘
```

### Key Patterns
| Pattern | Implementation |
|---------|----------------|
| **Minimal APIs** | `app.MapGet()`, `app.MapPost()`, etc. in `Program.cs` or route modules |
| **Delegate** | One per domain (`ITournamentDelegate`, `ITeamDelegate`, etc.) - orchestrates repositories, contains business rules |
| **Repository** | One per aggregate root - data access abstraction, testable via in-memory implementations |
| **Dependency Injection** | Built-in DI container, scoped per request |
| **Validation** | FluentValidation for request DTOs |
| **Serialization** | System.Text.Json with camelCase, enum as string |

---

## Endpoints

### Health Check
| Method | Path | Response | Description |
|--------|------|----------|-------------|
| `GET` | `/health` | `200` `"Services running"` | Service health probe |

---

### Teams

| Method | Path | Request Body | Response | Description |
|--------|------|--------------|----------|-------------|
| `GET` | `/teams` | - | `200` `TeamDto[]` | List all teams |
| `GET` | `/teams/{teamId}` | - | `200` `TeamDto` \| `400` (invalid ID) \| `404` | Get team by ID |
| `POST` | `/teams` | `CreateTeamDto` | `201` + `Location` \| `400` | Create new team |
| `PUT` | `/teams/{teamId}` | `UpdateTeamDto` | `200` `TeamDto` \| `404` | Full update team |
| `DELETE` | `/teams/{teamId}` | - | `204` \| `404` | Delete team |

**TeamDto**
```json
{
  "id": "string",
  "name": "string"
}
```

**CreateTeamDto**
```json
{
  "name": "string"
}
```

**UpdateTeamDto**
```json
{
  "name": "string"
}
```

---

### Tournaments

| Method | Path | Request Body | Response | Description |
|--------|------|--------------|----------|-------------|
| `GET` | `/tournaments` | - | `200` `TournamentDto[]` | List all tournaments |
| `GET` | `/tournaments/{tournamentId}` | - | `200` `TournamentDto` \| `404` | Get tournament by ID |
| `POST` | `/tournaments` | `CreateTournamentDto` | `201` + `Location` | Create tournament |
| `PUT` | `/tournaments/{tournamentId}` | `UpdateTournamentDto` | `200` `TournamentDto` \| `404` | Full update |
| `PATCH` | `/tournaments/{tournamentId}` | `PatchTournamentDto` | `200` `TournamentDto` \| `404` | Partial update |
| `DELETE` | `/tournaments/{tournamentId}` | - | `204` \| `404` | Delete tournament |

**TournamentDto**
```json
{
  "id": "string",
  "name": "string",
  "format": {
    "maxTeamsPerGroup": "integer",
    "numberOfGroups": "integer",
    "type": "ROUND_ROBIN" | "NFL"
  },
  "groups": "GroupDto[]",
  "matches": "MatchDto[]"
}
```

**CreateTournamentDto**
```json
{
  "name": "string",
  "format": {
    "maxTeamsPerGroup": "integer",
    "numberOfGroups": "integer",
    "type": "ROUND_ROBIN" | "NFL"
  }
}
```

**UpdateTournamentDto** (all fields required)
```json
{
  "name": "string",
  "format": {
    "maxTeamsPerGroup": "integer",
    "numberOfGroups": "integer",
    "type": "ROUND_ROBIN" | "NFL"
  }
}
```

**PatchTournamentDto** (all optional)
```json
{
  "name": "string?",
  "format": {
    "maxTeamsPerGroup": "integer?",
    "numberOfGroups": "integer?",
    "type": "ROUND_ROBIN | NFL?"
  }
}
```

---

### Groups

| Method | Path | Request Body | Response | Description |
|--------|------|--------------|----------|-------------|
| `GET` | `/tournaments/{tournamentId}/groups` | - | `200` `GroupDto[]` | List groups in tournament |
| `GET` | `/tournaments/{tournamentId}/groups/{groupId}` | - | `200` `GroupDto` \| `404` | Get group by ID |
| `POST` | `/tournaments/{tournamentId}/groups` | `CreateGroupDto` | `201` + `Location` \| `422` | Create group |
| `PUT` | `/tournaments/{tournamentId}/groups/{groupId}` | `UpdateGroupDto` | `200` `GroupDto` \| `404` | Full update group |
| `DELETE` | `/tournaments/{tournamentId}/groups/{groupId}` | - | `204` \| `404` | Delete group |
| `PATCH` | `/tournaments/{tournamentId}/groups/{groupId}/teams` | `AssignTeamsDto` | `204` \| `422` | Assign teams to group |

**GroupDto**
```json
{
  "id": "string",
  "name": "string",
  "tournamentId": "string",
  "teams": "TeamDto[]"
}
```

**CreateGroupDto**
```json
{
  "name": "string"
}
```

**UpdateGroupDto**
```json
{
  "name": "string"
}
```

**AssignTeamsDto**
```json
{
  "teamIds": ["string"]
}
```

---

### Matches

| Method | Path | Request Body | Response | Description |
|--------|------|--------------|----------|-------------|
| `GET` | `/tournaments/{tournamentId}/matches` | - | `200` `MatchDto[]` | List all matches in tournament |
| `GET` | `/tournaments/{tournamentId}/matches/{matchId}` | - | `200` `MatchDto` \| `404` | Get match by ID |
| `POST` | `/tournaments/{tournamentId}/matches` | `CreateMatchDto` | `201` + `Location` | Create match |
| `PATCH` | `/tournaments/{tournamentId}/matches/{matchId}/score` | `UpdateScoreDto` | `200` `MatchDto` \| `400` \| `404` | Update match score |
| `DELETE` | `/tournaments/{tournamentId}/matches/{matchId}` | - | `204` \| `404` | Delete match |

**MatchDto**
```json
{
  "id": "string",
  "tournamentId": "string",
  "groupId": "string?",
  "homeTeamId": "string",
  "visitorTeamId": "string",
  "homeTeam": "TeamDto?",
  "visitorTeam": "TeamDto?",
  "score": {
    "homeTeamScore": "integer",
    "visitorTeamScore": "integer"
  },
  "winner": "HOME | VISITOR | null",
  "isCompleted": "boolean"
}
```

**CreateMatchDto**
```json
{
  "groupId": "string?",
  "homeTeamId": "string",
  "visitorTeamId": "string"
}
```

**UpdateScoreDto**
```json
{
  "homeTeamScore": "integer",
  "visitorTeamScore": "integer"
}
```

---

## Error Responses

| Code | Scenario |
|------|----------|
| `400` | Invalid ID format, malformed JSON, validation failure (FluentValidation) |
| `404` | Resource not found (Team, Tournament, Group, Match) |
| `422` | Business logic violation (duplicate team in group, invalid score, etc.) |
| `500` | Unhandled exception |

**Error Response Format**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "fieldName": ["error message"]
  }
}
```

---

## Configuration (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tournament_db;Username=tournament_svc;Password=password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Testing Requirements

### Test Strategy Overview
| Layer | Test Type | Framework | Coverage Target |
|-------|-----------|-----------|-----------------|
| API Routes | Integration | xUnit + WebApplicationFactory | 100% endpoint coverage |
| Delegates | Unit | xUnit + Moq/NSubstitute | 90%+ branch coverage |
| Repositories | Unit + Integration | xUnit + EF Core InMemory / Testcontainers | 80%+ |
| Domain | Unit | xUnit | 100% business logic |

---

#### Required Test Cases per Endpoint

**Teams Routes** (`/teams`)
| Test Case | Description |
|-----------|-------------|
| `GET /teams` returns 200 with empty array | No teams in DB |
| `GET /teams` returns 200 with teams | Multiple teams exist |
| `GET /teams/{id}` returns 200 with team | Valid ID exists |
| `GET /teams/{id}` returns 404 | Valid ID not found |
| `GET /teams/{id}` returns 400 | Invalid ID format (non-alphanumeric) |
| `POST /teams` returns 201 + Location | Valid CreateTeamDto |
| `POST /teams` returns 400 | Missing name, empty name, duplicate name |
| `PUT /teams/{id}` returns 200 | Valid UpdateTeamDto |
| `PUT /teams/{id}` returns 404 | Valid ID not found |
| `DELETE /teams/{id}` returns 204 | Valid ID exists |
| `DELETE /teams/{id}` returns 404 | Valid ID not found |

**Tournaments Routes** (`/tournaments`)
| Test Case | Description |
|-----------|-------------|
| `GET /tournaments` returns 200 with array | Empty and populated |
| `GET /tournaments/{id}` returns 200 | With groups and matches populated |
| `GET /tournaments/{id}` returns 404 | Not found |
| `POST /tournaments` returns 201 + Location | Valid CreateTournamentDto (all format types) |
| `POST /tournaments` returns 400 | Invalid format, missing name, invalid type enum |
| `PUT /tournaments/{id}` returns 200 | Full update with format |
| `PUT /tournaments/{id}` returns 404 | Not found |
| `PATCH /tournaments/{id}` returns 200 | Partial update (name only, format only, both) |
| `PATCH /tournaments/{id}` returns 404 | Not found |
| `DELETE /tournaments/{id}` returns 204 | Cascades delete groups/matches |
| `DELETE /tournaments/{id}` returns 404 | Not found |

**Groups Routes** (`/tournaments/{tournamentId}/groups`)
| Test Case | Description |
|-----------|-------------|
| `GET /tournaments/{id}/groups` returns 200 | Empty and populated |
| `GET /tournaments/{id}/groups/{gid}` returns 200 | With teams populated |
| `GET /tournaments/{id}/groups/{gid}` returns 404 | Tournament or group not found |
| `POST /tournaments/{id}/groups` returns 201 | Valid name |
| `POST /tournaments/{id}/groups` returns 404 | Tournament not found |
| `POST /tournaments/{id}/groups` returns 422 | Duplicate group name in tournament |
| `PUT /tournaments/{id}/groups/{gid}` returns 200 | Valid update |
| `PUT /tournaments/{id}/groups/{gid}` returns 404 | Not found |
| `DELETE /tournaments/{id}/groups/{gid}` returns 204 | Removes group, unassigns teams |
| `PATCH /tournaments/{id}/groups/{gid}/teams` returns 204 | Assign valid team IDs |
| `PATCH /tournaments/{id}/groups/{gid}/teams` returns 422 | Team not in tournament, duplicate, exceeds maxTeamsPerGroup |
| `PATCH /tournaments/{id}/groups/{gid}/teams` returns 404 | Tournament or group not found |

**Matches Routes** (`/tournaments/{tournamentId}/matches`)
| Test Case | Description |
|-----------|-------------|
| `GET /tournaments/{id}/matches` returns 200 | Empty and populated |
| `GET /tournaments/{id}/matches/{mid}` returns 200 | With home/visitor team details |
| `GET /tournaments/{id}/matches/{mid}` returns 404 | Not found |
| `POST /tournaments/{id}/matches` returns 201 | Valid home/visitor team IDs (same tournament) |
| `POST /tournaments/{id}/matches` returns 422 | Team not in tournament, same team for home/visitor, team not in same group |
| `PATCH /tournaments/{id}/matches/{mid}/score` returns 200 | Valid scores, computes winner correctly |
| `PATCH /tournaments/{id}/matches/{mid}/score` returns 400 | Negative scores, non-integer |
| `PATCH /tournaments/{id}/matches/{mid}/score` returns 404 | Match not found |
| `DELETE /tournaments/{id}/matches/{mid}` returns 204 | Valid ID |
| `DELETE /tournaments/{id}/matches/{mid}` returns 404 | Not found |

#### Cross-Cutting Route Tests
| Test Case | Description |
|-----------|-------------|
| All endpoints return 401/403 | When auth middleware enabled (future) |
| Request validation | FluentValidation errors return 400 with ProblemDetails |
| Content-Type | All endpoints require/return `application/json` |
| ID format validation | All `{id}` params reject non-matching regex |
| Concurrency | Parallel requests don't cause data corruption |

---

### Delegate Tests (Unit Tests)

#### Setup Pattern
```csharp
public class TournamentDelegateTests
{
    private readonly Mock<ITournamentRepository> _repoMock;
    private readonly Mock<IGroupRepository> _groupRepoMock;
    private readonly Mock<IMatchRepository> _matchRepoMock;
    private readonly ITournamentDelegate _delegate;

    public TournamentDelegateTests()
    {
        _repoMock = new Mock<ITournamentRepository>();
        _groupRepoMock = new Mock<IGroupRepository>();
        _matchRepoMock = new Mock<IMatchRepository>();
        
        _delegate = new TournamentDelegate(
            _repoMock.Object, 
            _groupRepoMock.Object, 
            _matchRepoMock.Object);
    }
}
```

#### Required Test Cases per Delegate

**TournamentDelegate**
| Method | Test Cases |
|--------|------------|
| `GetByIdAsync` | Returns DTO when found, null when not found, maps format/groups/matches |
| `GetAllAsync` | Returns empty list, returns multiple with correct mapping |
| `CreateAsync` | Generates ID, saves via repo, returns ID, publishes event |
| `UpdateAsync` | Updates all fields, returns updated DTO, throws if not found |
| `PatchAsync` | Updates only provided fields, preserves others, throws if not found |
| `DeleteAsync` | Deletes tournament, cascades deletes groups & matches, publishes event |

**TeamDelegate**
| Method | Test Cases |
|--------|------------|
| `GetByIdAsync` | Returns DTO, null when not found |
| `GetAllAsync` | Empty list, multiple teams |
| `CreateAsync` | Generates ID, saves, returns ID, validates unique name |
| `UpdateAsync` | Updates name, throws if not found, validates unique name |
| `DeleteAsync` | Deletes team, throws if not found, handles referential integrity |

**GroupDelegate**
| Method | Test Cases |
|--------|------------|
| `GetByTournamentAsync` | Returns groups for tournament, empty if none |
| `GetByIdAsync` | Returns group with teams, null if not found |
| `CreateAsync` | Generates ID, validates tournament exists, enforces unique name per tournament |
| `UpdateAsync` | Updates name, validates unique name, throws if not found |
| `DeleteAsync` | Deletes group, unassigns teams, throws if not found |
| `AssignTeamsAsync` | Validates all teams exist, in same tournament, not already assigned, respects maxTeamsPerGroup |

**MatchDelegate**
| Method | Test Cases |
|--------|------------|
| `GetByTournamentAsync` | Returns matches with team details |
| `GetByIdAsync` | Returns match with computed winner/isCompleted |
| `CreateAsync` | Validates both teams in tournament, optionally in same group, generates ID |
| `UpdateScoreAsync` | Updates score, computes Winner, sets IsCompleted, validates non-negative |
| `DeleteAsync` | Deletes match, throws if not found |

---

### Test Data Builders
```csharp
// Fluent builders for test data
public class TournamentBuilder
{
    private Tournament _tournament = new();
    
    public TournamentBuilder WithId(string id) { _tournament.Id = id; return this; }
    public TournamentBuilder WithName(string name) { _tournament.Name = name; return this; }
    public TournamentBuilder WithFormat(TournamentFormat format) { _tournament.Format = format; return this; }
    public TournamentBuilder WithGroup(Group group) { _tournament.Groups.Add(group); return this; }
    public TournamentBuilder WithMatch(Match match) { _tournament.Matches.Add(match); return this; }
    public Tournament Build() => _tournament;
}
```

---

### Test Execution Requirements
| Requirement | Detail |
|-------------|--------|
| **CI Pipeline** | All tests run on every PR |
| **Coverage** | `dotnet test --collect:"XPlat Code Coverage"` with thresholds |
| **Parallelization** | Tests must be parallelizable (no shared state) |
| **Determinism** | No flaky tests; use fixed dates/IDs in tests |
| **Isolation** | Each test creates own data; no test ordering dependency |

---

### Test Project Structure
```
tests/
├── TournamentServices.Api.Tests/
│   ├── Routes/
│   │   ├── TeamRoutesTests.cs
│   │   ├── TournamentRoutesTests.cs
│   │   ├── GroupRoutesTests.cs
│   │   └── MatchRoutesTests.cs
│   ├── Middleware/
│   │   └── ValidationMiddlewareTests.cs
│   └── TournamentApiTests.cs (base class)
├── TournamentServices.Delegates.Tests/
│   ├── TournamentDelegateTests.cs
│   ├── TeamDelegateTests.cs
│   ├── GroupDelegateTests.cs
│   └── MatchDelegateTests.cs
├── TournamentServices.Repositories.Tests/
│   ├── TournamentRepositoryTests.cs
│   ├── TeamRepositoryTests.cs
│   ├── GroupRepositoryTests.cs
│   └── MatchRepositoryTests.cs
└── TournamentServices.Domain.Tests/
    ├── TournamentTests.cs
    ├── TeamTests.cs
    ├── GroupTests.cs
    ├── MatchTests.cs
    └── ScoreTests.cs
```

---

## Project Structure
```
TournamentServices/
├── src/
│   ├── TournamentServices.Api/
│   │   ├── Program.cs                    # Minimal API setup, DI registration
│   │   ├── Routes/
│   │   │   ├── TeamRoutes.cs
│   │   │   ├── TournamentRoutes.cs
│   │   │   ├── GroupRoutes.cs
│   │   │   └── MatchRoutes.cs
│   │   ├── Dtos/
│   │   │   ├── TeamDtos.cs
│   │   │   ├── TournamentDtos.cs
│   │   │   ├── GroupDtos.cs
│   │   │   └── MatchDtos.cs
│   │   ├── Validators/
│   │   └── Extensions/
│   ├── TournamentServices.Delegates/
│   │   ├── ITournamentDelegate.cs
│   │   ├── TournamentDelegate.cs
│   │   ├── ITeamDelegate.cs
│   │   ├── TeamDelegate.cs
│   │   ├── IGroupDelegate.cs
│   │   ├── GroupDelegate.cs
│   │   ├── IMatchDelegate.cs
│   │   └── MatchDelegate.cs
│   ├── TournamentServices.Repositories/
│   │   ├── ITournamentRepository.cs
│   │   ├── TournamentRepository.cs
│   │   ├── ITeamRepository.cs
│   │   ├── TeamRepository.cs
│   │   ├── IGroupRepository.cs
│   │   ├── GroupRepository.cs
│   │   ├── IMatchRepository.cs
│   │   └── MatchRepository.cs
│   └── TournamentServices.Domain/
│       ├── Tournament.cs
│       ├── Team.cs
│       ├── Group.cs
│       ├── Match.cs
│       ├── Score.cs
│       └── Enums/
├── tests/
│   ├── TournamentServices.Api.Tests/
│   ├── TournamentServices.Delegates.Tests/
│   └── TournamentServices.Repositories.Tests/
└── TournamentServices.sln
```
