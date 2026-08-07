# TableFlow

TableFlow is a restaurant reservation management API built with ASP.NET Core.

The API manages restaurants, tables, and reservations using SQL Server and Entity Framework Core.

## Current Features

### Restaurants

- List, filter, create, update, and delete restaurants
- Filter restaurants by city and cuisine type
- List active restaurants
- Persistent storage with SQL Server

### Tables

- List, create, update, and delete restaurant tables
- Filter tables by restaurant
- List active tables
- Validate restaurant relationships
- Prevent duplicate table numbers within the same restaurant

### Reservations

- List reservations
- Filter by restaurant, table, and status
- List upcoming reservations
- Create and update reservations
- Confirm and cancel reservations
- Validate restaurant and table relationships
- Prevent invalid status transitions

## API Design

The API currently includes:

- REST-oriented endpoints
- Typed request and response DTOs
- Dependency Injection
- Controllers and Services
- Asynchronous database access
- Entity Framework Core
- SQL Server persistence
- Fluent API configuration
- Database migrations
- Application-level operation results
- Consistent HTTP status codes
- Error responses using `ProblemDetails`
- Swagger/OpenAPI documentation

## Main HTTP Responses

| Scenario | Response |
|---|---|
| Successful query or update | `200 OK` |
| Resource created | `201 Created` |
| Successful deletion | `204 No Content` |
| Invalid input | `400 Bad Request` |
| Resource not found | `404 Not Found` |
| Data or state conflict | `409 Conflict` |

## Main Endpoints

### Restaurants

```http
GET    /restaurants
GET    /restaurants/{id}
GET    /restaurants/city/{city}
GET    /restaurants/cuisine/{cuisineType}
GET    /restaurants/active
POST   /restaurants
PUT    /restaurants/{id}
DELETE /restaurants/{id}
Tables
GET    /tables
GET    /tables/{id}
GET    /tables/restaurant/{restaurantId}
GET    /tables/active
POST   /tables
PUT    /tables/{id}
DELETE /tables/{id}
Reservations
GET /reservations
GET /reservations/{id}
GET /reservations/restaurant/{restaurantId}
GET /reservations/table/{tableId}
GET /reservations/status/{status}
GET /reservations/upcoming
POST /reservations
PUT /reservations/{id}
PUT /reservations/{id}/confirm
PUT /reservations/{id}/cancel
Project Structure
TableFlow
├── src
│   └── TableFlow.Api
│       ├── Controllers
│       ├── Data
│       ├── DTOs
│       ├── Entities
│       ├── Interfaces
│       ├── Migrations
│       ├── Models
│       ├── Services
│       └── Program.cs
├── TableFlow.sln
└── README.md
Request Flow
HTTP Request
    ↓
Controller
    ↓
Service Interface
    ↓
Service
    ↓
Entity Framework Core
    ↓
SQL Server
    ↓
Application Result
    ↓
HTTP Response
Tech Stack
    • C#
    • .NET 10
    • ASP.NET Core Web API
    • Entity Framework Core
    • SQL Server
    • Swagger / OpenAPI
    • Dependency Injection
    • Git
Running Locally
Requirements
    • .NET SDK
    • SQL Server
    • SQL Server Management Studio
    • Git
Setup
git clone https://github.com/bytecraftaibrazil/TableFlow.git
cd TableFlow
dotnet restore
dotnet build
Configure the SQL Server connection string in the API configuration file.
Apply the migrations:
dotnet ef database update \
    --project src/TableFlow.Api \
    --startup-project src/TableFlow.Api
Start the API using the Visual Studio Code debugger or the .NET CLI.
Open the Swagger endpoint after the application starts:
/swagger
Roadmap
The next stages include:
    1. Advanced SQL and Entity Framework Core queries
    2. Pagination, indexes, transactions, and concurrency
    3. Reservation availability and scheduling rules
    4. React and TypeScript administrative interface
    5. Clean Architecture
    6. CQRS and MediatR
    7. Authentication, authorization, and automated tests
    8. Docker, CI/CD, Azure, and production configuration
Status
Under active development.
Current stage: Phase 2 — Data

---