# TableFlow

TableFlow is a restaurant reservation management API built with ASP.NET Core.

The project provides a consistent HTTP API for managing restaurants, tables, and reservations. It is currently in the Foundation phase, using in-memory data while the API contract and application structure are consolidated.

## Current Features

### Restaurants

- List all restaurants
- Find a restaurant by ID
- Filter restaurants by city
- Filter restaurants by cuisine type
- List active restaurants
- Create restaurants
- Update restaurants
- Delete restaurants

### Tables

- List all tables
- Find a table by ID
- Filter tables by restaurant
- List active tables
- Create tables
- Update tables
- Delete tables

### Reservations

- List all reservations
- Find a reservation by ID
- Filter reservations by restaurant
- Create reservations
- Update reservation details
- Confirm reservations
- Cancel reservations

## API Design

The API currently includes:

- REST-oriented routes
- Typed request and response DTOs
- Service interfaces and Dependency Injection
- Separation between Controllers and Services
- Consistent HTTP status codes
- Error responses using `ProblemDetails`
- Swagger/OpenAPI documentation
- In-memory data storage

The main response patterns are:

| Scenario | HTTP response |
|---|---|
| Successful query or update | `200 OK` |
| Resource created | `201 Created` |
| Successful deletion | `204 No Content` |
| Invalid input | `400 Bad Request` |
| Resource not found | `404 Not Found` |

Creation endpoints return a `Location` header pointing to the newly created resource.

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
```

### Tables

```http
GET    /tables
GET    /tables/{id}
GET    /tables/restaurant/{restaurantId}
GET    /tables/active
POST   /tables
PUT    /tables/{id}
DELETE /tables/{id}
```

### Reservations

```http
GET /reservations
GET /reservations/{id}
GET /reservations/restaurant/{restaurantId}
POST /reservations
PUT /reservations/{id}
PUT /reservations/{id}/confirm
PUT /reservations/{id}/cancel
```

## Project Structure

```text
TableFlow
├── src
│   └── TableFlow.Api
│       ├── Controllers
│       ├── DTOs
│       ├── Interfaces
│       ├── Models
│       ├── Services
│       └── Program.cs
├── TableFlow.sln
└── README.md
```

The current request flow is:

```text
HTTP Request
    ↓
Controller
    ↓
Service Interface
    ↓
Service
    ↓
Application Result
    ↓
HTTP Response
```

Services return application-level results such as objects, collections, `null`, or `bool`. Controllers map those results to HTTP responses and status codes.

## Tech Stack

### Current

- C#
- .NET 8
- ASP.NET Core Web API
- Swagger / OpenAPI
- Swashbuckle
- Dependency Injection
- In-memory collections

### Planned

- SQL Server
- Entity Framework Core
- React
- TypeScript
- Clean Architecture
- CQRS
- MediatR
- JWT authentication and authorization
- Automated tests
- Docker
- Azure

## Running Locally

### Requirements

- .NET 8 SDK
- Git

### Setup

```bash
git clone https://github.com/bytecraftaibrazil/TableFlow.git
cd TableFlow
dotnet restore
dotnet run --project src/TableFlow.Api
```

After starting the API, open the Swagger URL displayed in the terminal and append:

```text
/swagger
```

Example:

```text
http://localhost:PORT/swagger
```

The port may change depending on the local launch configuration.

## Data Persistence

The current implementation stores data in memory.

This means that:

- no database configuration is required;
- the data is reset whenever the application restarts;
- SQL Server and Entity Framework Core will be introduced in the Data phase.

## Roadmap

The next major stages include:

1. Persisting data with SQL Server and Entity Framework Core
2. Implementing reservation availability and conflict rules
3. Building a React and TypeScript administrative interface
4. Evolving the architecture with Clean Architecture, CQRS, and MediatR
5. Adding authentication, authorization, logs, tests, Docker, and Azure deployment

## Status

🚧 **Under active development**

Current stage: **Phase 1 — Foundation Consolidation**
