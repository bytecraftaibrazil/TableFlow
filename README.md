# TableFlow

TableFlow is a restaurant reservation management API built with ASP.NET Core.

The API manages restaurants, tables, and reservations using SQL Server and Entity Framework Core.

It includes persistence, relational integrity, reservation business rules, table selection, availability queries, overlap detection, conflict prevention, filtering, pagination, and database-level constraints.

---

## Current Features

### Restaurants

- List restaurants
- Get restaurant by id
- Filter restaurants by city
- Filter restaurants by cuisine type
- List active restaurants
- Create restaurants
- Update restaurants
- Delete restaurants
- Persistent storage with SQL Server

### Tables

- List restaurant tables
- Get table by id
- Filter tables by restaurant
- List active tables
- Create tables
- Update tables
- Delete tables
- Validate restaurant relationships
- Prevent duplicate table numbers inside the same restaurant
- Enforce table number and capacity constraints
- Select a suitable table using a Best Fit strategy

### Reservations

- List reservations
- Get reservation by id
- Filter reservations by restaurant
- Filter reservations by table
- Filter reservations by status
- List upcoming reservations
- List upcoming confirmed reservations
- List upcoming pending reservations
- Search reservations with filtering and pagination
- Create reservations
- Update reservations
- Confirm reservations
- Cancel reservations
- Store reservation duration
- Derive reservation end time from start time and duration
- Validate restaurant and table relationships
- Validate active restaurants and tables
- Validate party size against table capacity
- Query candidate and available tables
- Detect overlapping reservation intervals
- Prevent conflicting reservations
- Allow adjacent non-overlapping reservations
- Ignore cancelled reservations when checking availability
- Prevent invalid reservation status transitions
- Prevent updates to cancelled reservations

---

## Reservation Engine

Reservation business rules are centralized in the reservation engine.

The engine is responsible for:

```text
Restaurant validation
↓
Table validation
↓
Capacity validation
↓
Duration validation
↓
Suitable table selection
↓
Availability calculation
↓
Overlap detection
↓
Conflict detection
```

The service layer coordinates application operations and persistence, while controllers handle HTTP concerns.

Current request flow:

```text
HTTP Request
↓
Controller
↓
Service Interface
↓
Service
↓
Reservation Engine
↓
Entity Framework Core
↓
SQL Server
↓
Application Result
↓
Controller
↓
HTTP Response
```

---

## Table Selection

TableFlow provides a suggested-table endpoint:

```http
GET /reservations/suggested-table
```

Example:

```http
GET /reservations/suggested-table?restaurantId=1&partySize=4
```

The selection strategy uses Best Fit:

```text
Active restaurant
↓
Active tables
↓
Capacity >= PartySize
↓
Order by Capacity
↓
Then by Table Number
↓
Then by Id
↓
First result
```

This minimizes unused table capacity while keeping the result deterministic.

---

## Reservation Availability

TableFlow provides an availability endpoint:

```http
GET /reservations/availability
```

Example:

```http
GET /reservations/availability?restaurantId=1&partySize=4&reservationDate=2026-10-15T19:00:00&durationMinutes=90
```

The response separates:

```text
CandidateTables
AvailableTables
```

### Candidate Tables

Candidate tables satisfy structural reservation rules:

- belong to the requested restaurant;
- restaurant is active;
- table is active;
- table capacity is greater than or equal to the requested party size.

### Available Tables

Available tables are candidate tables that do not have a blocking reservation during the requested interval.

Blocking statuses currently include:

```text
Pending
Confirmed
```

Cancelled reservations do not block table availability.

---

## Reservation Time Model

A reservation stores:

```text
ReservationDate
DurationMinutes
```

The end time is derived:

```text
ReservationEnd = ReservationDate + DurationMinutes
```

`ReservationEnd` is not stored separately in the database.

This avoids redundant temporal data.

---

## Overlap Detection

TableFlow uses half-open reservation intervals:

```text
[start, end)
```

Two reservations overlap when:

```text
existingStart < requestedEnd
&&
requestedStart < existingEnd
```

Example conflict:

```text
Existing:  19:00 → 20:30
Requested: 19:30 → 21:00
```

Example without conflict:

```text
Existing:  19:00 → 20:00
Requested: 20:00 → 21:00
```

A reservation may start exactly when another reservation ends.

Conflicting create and update operations return:

```http
409 Conflict
```

Updates exclude the reservation being modified from their own conflict check.

---

## Reservation Search

TableFlow provides a paginated reservation search endpoint:

```http
GET /reservations/search
```

Supported filters:

- `status`
- `restaurantId`
- `tableId`
- `fromDate`
- `toDate`
- `minimumPartySize`
- `descending`
- `pageNumber`
- `pageSize`

Example:

```http
GET /reservations/search?status=Confirmed&restaurantId=1&pageNumber=1&pageSize=20
```

The search response contains:

```text
Items
PageNumber
PageSize
TotalCount
TotalPages
HasNextPage
```

Pagination is executed on the database side using:

```text
Filters
↓
CountAsync
↓
OrderBy / ThenBy
↓
Skip
↓
Take
↓
Projection
↓
ToListAsync
```

---

## API Design

The API currently uses:

- REST-oriented endpoints
- Controllers
- Service interfaces
- Service implementations
- Reservation business-rule engine
- Dependency Injection
- Typed request and response DTOs
- Application-level operation results
- Async database operations
- Consistent HTTP status codes
- `ProblemDetails` error responses
- Swagger / OpenAPI documentation

---

## Data Layer

TableFlow uses:

- SQL Server
- Entity Framework Core
- `DbContext`
- `DbSet`
- Fluent API
- Migrations
- Primary Keys
- Foreign Keys
- Navigation Properties
- Restricted delete behavior
- CHECK constraints
- Unique indexes
- Composite indexes
- LINQ queries
- Query projection
- `AsNoTracking`
- Async database access

---

## Database Relationships

The current model contains the following relationships:

```text
Restaurant
    1
    │
    ├────────── N RestaurantTable
    │
    └────────── N Reservation

RestaurantTable
    1
    │
    └────────── N Reservation
```

Reservations reference both:

```text
RestaurantId
TableId
```

Delete behavior is configured with:

```text
DeleteBehavior.Restrict
```

to prevent unintended cascade deletion.

---

## Database Constraints

The database enforces important integrity rules.

### Tables

```text
Number > 0
Capacity > 0
Capacity <= 50
```

Constraint names:

```text
CK_Tables_Number_Positive
CK_Tables_Capacity_Positive
CK_Tables_Capacity_Maximum
```

Table numbers must also be unique inside the same restaurant:

```text
(RestaurantId, Number)
```

### Reservations

```text
PartySize > 0
DurationMinutes > 0
```

Valid reservation statuses are:

```text
Pending
Confirmed
Cancelled
```

Constraint names:

```text
CK_Reservations_PartySize_Positive
CK_Reservations_DurationMinutes_Positive
CK_Reservations_Status_Valid
```

---

## Reservation Indexes

TableFlow currently includes composite indexes designed around reservation query patterns.

### Status and reservation date

```text
(Status, ReservationDate)
```

Index:

```text
IX_Reservations_Status_ReservationDate
```

This supports queries that filter reservations by status and work with reservation dates.

### Restaurant and reservation date

```text
(RestaurantId, ReservationDate)
```

Index:

```text
IX_Reservations_RestaurantId_ReservationDate
```

This supports reservation queries scoped to a restaurant and ordered or filtered by date.

Indexes are maintained through Entity Framework Core migrations.

---

## Main HTTP Responses

| Scenario | HTTP Response |
|---|---|
| Successful query | `200 OK` |
| Successful update | `200 OK` |
| Resource created | `201 Created` |
| Successful deletion | `204 No Content` |
| Invalid input | `400 Bad Request` |
| Resource not found | `404 Not Found` |
| Reservation or state conflict | `409 Conflict` |

Collection and availability endpoints return:

```http
200 OK
```

with empty collections when no items match.

---

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
GET /reservations/table/{tableId}
GET /reservations/status/{status}
GET /reservations/upcoming
GET /reservations/upcoming/confirmed
GET /reservations/upcoming/pending
GET /reservations/search
GET /reservations/suggested-table
GET /reservations/availability

POST /reservations

PUT /reservations/{id}
PUT /reservations/{id}/confirm
PUT /reservations/{id}/cancel
```

---

## Reservation Status Flow

New reservations are created with:

```text
Pending
```

Supported transitions currently include:

```text
Pending
├── Confirmed
└── Cancelled
```

A confirmed reservation may be cancelled.

A cancelled reservation:

```text
cannot be confirmed
cannot be updated
does not block table availability
```

Repeated confirmation or cancellation operations are handled safely by the service behavior.

---

## Query Strategy

Read-only queries use patterns such as:

```csharp
.AsNoTracking()
```

Filtering and ordering are composed through LINQ before query materialization.

Example flow:

```text
IQueryable
↓
Where
↓
OrderBy
↓
Select
↓
ToListAsync
```

Queries are executed only when materialization or database operations occur, such as:

```text
ToListAsync
CountAsync
FirstOrDefaultAsync
AnyAsync
```

Suitable-table selection is shared between table suggestion and availability operations.

---

## Migrations

Database structure is maintained through Entity Framework Core migrations.

Current migration history includes changes for:

- Initial database structure
- Data integrity constraints
- Maximum table capacity
- Reservation search indexes
- Reservation duration

The reservation-duration migration preserves existing records by backfilling a valid duration before making the column required.

Typical workflow:

```text
Model change
↓
dotnet ef migrations add
↓
Migration generated
↓
dotnet ef database update
↓
SQL Server updated
```

---

## Sample Data

The repository includes:

```text
scripts/seed-large.sql
```

The script generates related sample data for local development and query analysis.

It creates approximately:

```text
10 restaurants
80 tables
480 reservations
```

The script is designed to avoid duplicating its own seeded records when executed again.

---

## Project Structure

```text
TableFlow
├── scripts
│   └── seed-large.sql
│
├── src
│   └── TableFlow.Api
│       ├── Controllers
│       ├── Data
│       │   ├── Migrations
│       │   └── TableFlowDbContext.cs
│       ├── DTOs
│       ├── Entities
│       ├── Interfaces
│       ├── Models
│       ├── Services
│       ├── Program.cs
│       └── TableFlow.Api.csproj
│
├── global.json
├── TableFlow.sln
└── README.md
```

---

## Tech Stack

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Swagger / OpenAPI
- Dependency Injection
- Git

---

## Running Locally

### Requirements

Install:

- .NET 10 SDK
- SQL Server
- SQL Server Management Studio
- Git
- Visual Studio Code

### Clone the repository

```bash
git clone https://github.com/bytecraftaibrazil/TableFlow.git
cd TableFlow
```

### Restore dependencies

```bash
dotnet restore
```

### Build the solution

```bash
dotnet build
```

### Database Connection

Configure the SQL Server connection string in:

```text
src/TableFlow.Api/appsettings.Development.json
```

The application expects the connection string:

```text
TableFlowDatabase
```

### Apply migrations

From the repository root:

```bash
dotnet ef database update \
    --project src/TableFlow.Api \
    --startup-project src/TableFlow.Api
```

### Start the API

The project is configured to run through the Visual Studio Code debugger.

Use:

```text
F5
```

After the application starts, open:

```text
/docs
```

to access Swagger UI.

---

## Development Workflow

The current development workflow is:

```text
Change
↓
Build
↓
Run with F5
↓
Swagger smoke test
↓
Database verification when necessary
↓
Commit
↓
Push
```

Useful commands:

```bash
dotnet restore
dotnet build
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

## Current Architecture

TableFlow currently follows a modular monolith structure inside a single ASP.NET Core application.

```text
TableFlow.Api
├── Controllers
├── DTOs
├── Interfaces
├── Models
├── Services
├── Data
├── Entities
└── Program.cs
```

The application currently separates HTTP handling, orchestration, reservation rules, persistence, and data access while remaining inside a single project.

---

## Next Technical Stages

Planned technical evolution includes:

1. Clean Architecture
2. Dependency boundaries between Domain, Application, Infrastructure, and API
3. CQRS and MediatR
4. Reservation status workflow improvements
5. Unit and integration testing
6. Authentication and authorization
7. Global exception handling
8. Structured logging and correlation
9. Health checks and rate limiting
10. Docker and Docker Compose
11. GitHub Actions
12. Azure deployment
13. Caching and Redis
14. Idempotency and concurrency handling
15. Background processing and resiliency
16. System Design improvements

---

## Status

```text
Project: Active

Current architecture:
Modular Monolith

Current persistence:
SQL Server + Entity Framework Core

Current reservation capabilities:
Table selection + availability + overlap detection + conflict prevention

Current technical stage:
Preparing for architectural restructuring
```

---
