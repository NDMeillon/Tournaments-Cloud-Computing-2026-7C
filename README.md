# Tournaments_V1

A lightweight Tournament Management REST API built with C# and .NET Minimal APIs, designed using a top-down architecture with contract validation and automated testing.

---

## Tech Stack

* **Runtime:** .NET 9 / 10+
* **Framework:** ASP.NET Core Minimal APIs
* **Validation:** FluentValidation
* **Testing:** xUnit, `Microsoft.AspNetCore.Mvc.Testing`, FluentAssertions

---

## Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) (version 9.0 or later)
* Visual Studio Code (or preferred editor)

### Run the API

```bash
# From the project root
dotnet run --project Tournaments_V1
```

The API will start listening at: `http://localhost:8080`
### Run tests.
```bash
# Run unit and integration test suites
dotnet test
```

### API Specification
All request and response bodies use `application/json.` All identifier parameters (`id`, `groupId`, etc.) must match the pattern `^[A-Za-z0-9\-]+$`.

### Structure 
```plaintext
Tournaments_V1/
├── Common/Filters/       # Endpoint filters (e.g., ValidationFilter)
├── Contracts/            # Request and Response DTO records
│   ├── Requests/
│   └── Responses/
├── Endpoints/            # Route groups and handler mappings
├── Validators/           # FluentValidation rules
├── appsettings.json      # Server configuration and ports
└── Program.cs            # DI service registration and middleware pipeline

Tournaments_V1.Tests/
├── Endpoints/            # Integration tests via WebApplicationFactory
└── Validators/           # Unit tests for FluentValidation rules
```
