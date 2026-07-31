# TableFlow

TableFlow is a restaurant reservation management API built with ASP.NET Core.

The API provides modules for managing restaurants, tables, and reservations through consistent HTTP contracts.

## Current Features

### Restaurants

- List, filter, create, update, and delete restaurants
- Filter by city and cuisine type
- List active restaurants

### Tables

- List, create, update, and delete tables
- Filter by restaurant
- List active tables

### Reservations

- List and create reservations
- Filter by restaurant
- Update reservation details
- Confirm and cancel reservations

## API Design

The API currently includes:

- REST-oriented routes
- Typed request and response DTOs
- Controller and Service separation
- Dependency Injection
- Consistent HTTP status codes
- Error responses using `ProblemDetails`
- Swagger/OpenAPI documentation

Creation endpoints return `201 Created` with a `Location` header.

## Main Endpoints

```http
GET    /restaurants
GET    /restaurants/{id}
POST   /restaurants
PUT    /restaurants/{id}
DELETE /restaurants/{id}

GET    /tables
GET    /tables/{id}
POST   /tables
PUT    /tables/{id}
DELETE /tables/{id}

GET  /reservations
GET  /reservations/{id}
POST /reservations
PUT  /reservations/{id}
PUT  /reservations/{id}/confirm
PUT  /reservations/{id}/cancel
Project Structure
TableFlow
└── src
    └── TableFlow.Api
        ├── Controllers
        ├── Data
        │   ├── Migrations
        │   └── TableFlowDbContext.cs
        ├── DTOs
        ├── Entities
        ├── Interfaces
        ├── Models
        ├── Services
        └── Program.cs
Tech Stack
    • C#
    • .NET 10
    • ASP.NET Core Web API
    • Entity Framework Core
    • SQL Server
    • Swagger / OpenAPI
    • Swashbuckle
    • Dependency Injection
Data Persistence
The SQL Server database schema is managed through Entity Framework Core migrations.
The current schema includes:
    • Restaurants
    • Tables
    • Reservations
    • __EFMigrationsHistory
The database relationships and constraints are configured, while the application services are being migrated incrementally from in-memory collections to database persistence.
Running Locally
Requirements:
    • .NET 10 SDK
    • SQL Server
    • SQL Server Management Studio
    • Git
Restore and build:
dotnet restore
dotnet build
Run the API through the VS Code debugger or:
dotnet run --project src/TableFlow.Api
Apply pending migrations:
dotnet ef database update \
  --project src/TableFlow.Api \
  --startup-project src/TableFlow.Api
Roadmap
    1. Integrate application services with Entity Framework Core
    2. Implement reservation availability and conflict rules
    3. Build a React and TypeScript administrative interface
    4. Evolve the architecture with Clean Architecture, CQRS, and MediatR
    5. Add authentication, tests, Docker, and Azure deployment
Status
Under active development.
Current stage: Phase 2 — Data