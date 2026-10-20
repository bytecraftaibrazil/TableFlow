# TableFlow — Business Rules and Service Test Reference

**Project:** TableFlow — Restaurant Reservation Management API  
**Scope:** `RestaurantService`, `TableService`, `ReservationService`, and `ReservationEngine`  
**Technology:** C# / .NET 10 / ASP.NET Core / Entity Framework Core / SQL Server  
**Purpose:** Document the business behavior and provide an accurate reference for writing service tests.

> **Source of truth:** This document reflects the local implementation reviewed on October 20, 2026, including commits `fc10def` (reservation reliability) and `b7b3fcb` (mocked unit tests). It describes implemented behavior separately from remaining limitations. Review it whenever the code changes; GitHub publication status does not determine the local business contract.
>
> **Scope guard:** Focus on existing business rules and tests. Do **not** introduce Docker, microservices, CQRS, MediatR, Clean Architecture, a frontend, or unrelated features.

## 1. Business Domain

The system manages three related entities:

- **Restaurant:** A restaurant with a name, cuisine type, city, and active/inactive status.
- **RestaurantTable:** A numbered table belonging to exactly one restaurant, with a seating capacity and active/inactive status.
- **Reservation:** A booking for one table at one restaurant, with a customer name, start date/time, duration, party size, and status.

Relationships:

```text
Restaurant (1) ---- (many) RestaurantTable
Restaurant (1) ---- (many) Reservation
RestaurantTable (1) ---- (many) Reservation
```

The reservation's `RestaurantId` and `TableId` must describe the same restaurant/table relationship.

## 2. Restaurant Rules — RestaurantService

| ID | Rule | Current enforcement |
| --- | --- | --- |
| REST-01 | A restaurant has `Name`, `CuisineType`, `City`, and `IsActive`. | Entity/service |
| REST-02 | Name, cuisine type, and city must be nonblank and each contain at least **3 characters after trimming**. | Controller validation |
| REST-03 | The values of name, cuisine type, and city are trimmed when a restaurant is created or updated. | Service |
| REST-04 | Database maximum lengths: name **120**, cuisine type **80**, city **80** characters. | EF Core / database |
| REST-05 | Creating a valid restaurant persists its values and returns a representation of the created restaurant. | Service |
| REST-06 | Updating a nonexistent restaurant returns `null`; updating an existing restaurant persists the new values. | Service |
| REST-07 | Deleting a nonexistent restaurant returns `false`; deleting an existing restaurant without dependencies returns `true` after successful persistence. | Service |
| REST-08 | Active-only queries return restaurants with `IsActive == true`. | Service |
| REST-09 | City and cuisine filters trim the filter string and use equality against the corresponding persisted value. Do not assume case-insensitivity independently of the SQL Server collation. | Service / database |
| REST-10 | Restaurant deletion must not cascade-delete related tables or reservations. Database relationships use restricted deletion. | EF Core / database |
| REST-11 | Deleting a restaurant with tables or reservations is rejected with `ResourceConflictException`, translated to HTTP `409` with `ProblemDetails`. | Service + exception handler |

**Deletion boundary:** The service checks dependencies before removal and catches SQL Server error `547` for the known foreign-key constraints if a dependency appears before `SaveChangesAsync` completes. Unrelated database errors are not translated into a dependency conflict. HTTP outcomes are `404` for a missing restaurant, `204` for successful deletion, and `409` for related records.

### Restaurant test scenarios

- Creating a restaurant trims all text values and persists the active flag.
- Updating an existing restaurant changes the intended fields.
- Updating/deleting an unknown ID returns the documented not-found result.
- Active filtering excludes inactive restaurants.
- Filtering by city and cuisine returns the correct records in the documented order.
- Deleting a restaurant with related records does not remove its dependent data (SQL Server integration test).

## 3. Table Rules — TableService

| ID | Rule | Current enforcement |
| --- | --- | --- |
| TAB-01 | Every table must reference an existing restaurant. | Service + foreign key |
| TAB-02 | The table number must be greater than **0**. | Controller + database CHECK constraint |
| TAB-03 | Table capacity must be between **1 and 50** people, inclusive. | Minimum: controller/database; maximum: database CHECK constraint |
| TAB-04 | Table numbers must be unique **within the same restaurant**. Different restaurants may reuse the same number. | Service + unique database index on `(RestaurantId, Number)` |
| TAB-05 | Tables can be active or inactive. Active-only queries return `IsActive == true`. | Service |
| TAB-06 | A create/update request referencing a nonexistent restaurant returns `RestaurantNotFound`. | Service |
| TAB-07 | A create/update request conflicting with another table number in the same restaurant returns `DuplicateNumber`. | Service |
| TAB-08 | Updating a table must not flag that same table's unchanged number as a duplicate. | Service (self-ID excluded) |
| TAB-09 | Updating a nonexistent table returns `TableNotFound`. | Service |
| TAB-10 | Deleting a nonexistent table returns `false`; deleting an existing table without reservations returns `true` after successful persistence. | Service |
| TAB-11 | Database relationships restrict deleting a table referenced by reservations. | EF Core / database |
| TAB-12 | Any linked reservation, including `Cancelled`, prevents table deletion. The service throws `ResourceConflictException`, translated to HTTP `409` with `ProblemDetails`. | Service + exception handler |

**Current limitations and deletion handling:**

- The service does not separately reject a table assigned to an **inactive** restaurant; reservation validation does reject bookings at inactive restaurants.
- The update method allows `RestaurantId` to change if the destination restaurant exists and the table number is unique, even when reservations reference the table. This can leave existing reservation records inconsistent with their restaurant/table relationship. No reassignment restriction is currently implemented; do not invent one in a test.
- Deletion checks reservations first and catches violations of known foreign-key constraints during persistence to handle the check/delete race. Missing tables return HTTP `404`; successful deletion returns `204`; dependency conflicts return `409`. Unrelated database errors propagate.

### Table test scenarios

- A table cannot be created for a nonexistent restaurant.
- Duplicate table numbers are rejected within one restaurant but accepted across two restaurants.
- An unchanged table number can be saved during an update.
- Updating an unknown table returns `TableNotFound`.
- Active-only filtering excludes inactive tables.
- Capacities `0`, `1`, `50`, and `51` cover the defined boundaries; database-enforced cases require an appropriate integration test.
- Deleting a referenced table does not cascade-delete reservations (SQL Server integration test).

## 4. Reservation Validation — ReservationEngine

The reservation engine validates a **candidate reservation** before creation or update.

| ID | Rule | Expected outcome |
| --- | --- | --- |
| ENG-01 | The restaurant must exist. | `RestaurantNotFound` otherwise |
| ENG-02 | The restaurant must be active. | `RestaurantInactive` otherwise |
| ENG-03 | The table must exist. | `TableNotFound` otherwise |
| ENG-04 | The table must belong to the specified restaurant. | `TableDoesNotBelongToRestaurant` otherwise |
| ENG-05 | The table must be active. | `TableInactive` otherwise |
| ENG-06 | The party size cannot exceed the table's capacity. | `PartySizeExceedsTableCapacity` otherwise |
| ENG-07 | Duration must be strictly greater than zero and calculating the end time must not exceed `DateTime.MaxValue`. | `ReservationDurationInvalid` otherwise |
| ENG-08 | A table cannot be booked for a time interval that overlaps an existing blocking reservation on that same table. | `ReservationConflict` otherwise |
| ENG-09 | When updating a reservation, its own ID is excluded from conflict detection. | No self-conflict |
| ENG-10 | If all validations succeed, return `Success`. | `Success` |
| ENG-11 | Start time must have `DateTimeKind.Utc`, be later than `DateTime.UtcNow`, and party size must be positive. These checks run before database lookups. | `ReservationInputInvalid` otherwise |

### 4.1 Overlapping intervals

For a reservation interval, define:

```text
start = ReservationDate
end   = ReservationDate + DurationMinutes
```

Intervals are **half-open** (`[start, end)`), so an interval ending exactly when another one starts is **not** a conflict.

```text
Overlaps = existingStart < requestedEnd
           && requestedStart < existingEnd
```

Examples for the **same table**:

| Existing booking | Requested booking | Result |
| --- | --- | --- |
| 18:00–19:30 | 19:00–20:00 | Conflict |
| 18:00–19:30 | 18:30–19:00 | Conflict |
| 18:00–19:30 | 17:30–18:30 | Conflict |
| 18:00–19:30 | 18:00–19:30 | Conflict |
| 18:00–19:30 | 19:30–20:30 | No conflict |
| 18:00–19:30 | 17:00–18:00 | No conflict |

Only **`Pending`** and **`Confirmed`** reservations block a time slot. A **`Cancelled`** reservation does not block it. Bookings for *different tables* do not conflict merely because their time intervals overlap.

**Current validation boundary:** Create/update controllers check positive IDs and party size, customer-name requirements, and a future UTC start. The engine independently checks UTC kind, future start, and positive party size, returning `ReservationInputInvalid` when these fail. Customer-name validation remains in the controller; the engine candidate has no customer-name field. Explicit positive-ID input checks also remain in the controller; the engine resolves restaurant/table existence through its lookups.

## 5. Table Suggestions and Availability — ReservationEngine

| ID | Rule | Current behavior |
| --- | --- | --- |
| AVL-01 | Only tables from the specified restaurant are considered. | Implemented |
| AVL-02 | Both the restaurant and candidate table must be active. | Implemented |
| AVL-03 | Candidate table capacity must be at least the party size. | Implemented |
| AVL-04 | Candidate tables are ordered by **capacity ascending**, then **table number ascending**, then **ID ascending**. | Implemented |
| AVL-05 | `SuggestTableAsync(restaurantId, partySize)` returns the first suitable table, or `null`. | Implemented |
| AVL-06 | A suggested table is **not guaranteed to be available for a given time**: that method has no date or duration parameter. | Current API limitation |
| AVL-07 | Availability results return both the list of suitable candidate tables and the subset that have no overlapping blocking reservations. | Implemented |
| AVL-08 | If there are no suitable candidates, both returned lists are empty. | Implemented |
| AVL-09 | Cancelled reservations do not exclude tables from the available subset. | Implemented |
| AVL-10 | Availability is advisory and does not hold or allocate a table; it can change before creation completes. | Only the write operation acquires reservation locks |

**Validation boundary:** The availability HTTP endpoint checks positive restaurant ID/party size/duration, a future UTC start, and date overflow. JSON/query binding requires an explicit time zone. `GetAvailabilityAsync` in the engine assumes it receives a valid request; an invalid direct call may throw when computing `ReservationEnd`. Do not treat controller checks as engine guarantees. `SuggestTableAsync` separately returns `null` for nonpositive restaurant ID or party size.

### Suggestion and availability test scenarios

- An eligible table exactly matching the party size is included.
- An undersized, inactive, or other-restaurant table is excluded.
- Sorting consistently picks the smallest suitable capacity and resolves ties by table number and ID.
- A blocked table appears in `CandidateTables` but not in `AvailableTables`.
- A table blocked only by cancelled reservations remains available.
- Back-to-back reservations do not make a table unavailable.
- An empty candidate set returns two empty lists.
- `SuggestTableAsync` can return a table with an existing reservation: suggestion is capacity-based, not time-based.

## 6. Reservation Lifecycle — ReservationService

| ID | Rule | Current behavior |
| --- | --- | --- |
| RES-01 | A successfully created reservation starts in `Pending`. | Implemented |
| RES-02 | Creation must call the reservation engine to validate the restaurant, table, capacity, duration, and time conflict. | Implemented |
| RES-03 | A rejected candidate must not be saved as a new reservation. | Implemented |
| RES-04 | Customer names are trimmed on creation and update. | Implemented |
| RES-05 | Updating a nonexistent reservation returns `ReservationNotFound`. | Implemented |
| RES-06 | Updating a `Cancelled` reservation is forbidden. | `CancelledReservationCannotBeUpdated` |
| RES-07 | Updating any other reservation revalidates its new details, excluding its own ID from overlap checks. | Implemented |
| RES-08 | Confirming `Pending` changes status to `Confirmed`. | Implemented |
| RES-09 | Confirming `Confirmed` again succeeds without changing the status. | Implemented; idempotent behavior |
| RES-10 | Confirming `Cancelled` is forbidden. | `InvalidStatusTransition` |
| RES-11 | Cancelling `Pending` or `Confirmed` changes status to `Cancelled`. | Implemented |
| RES-12 | Cancelling `Cancelled` again succeeds without changing the status. | Implemented; idempotent behavior |
| RES-13 | Confirming or cancelling an unknown reservation returns `ReservationNotFound`. | Implemented |
| RES-14 | Valid reservation statuses are `Pending`, `Confirmed`, and `Cancelled`. | Database CHECK constraint |
| RES-15 | Creation/update acquire transactional table locks before validating conflicts and saving. | `IReservationWriteSession` + SQL Server `sp_getapplock` |
| RES-16 | Updating, confirming, and cancelling first lock the reservation ID and reload its persisted state before making status decisions. | `IReservationWriteSession` |

### 6.1 Valid status transitions

```text
Pending --confirm--> Confirmed
   |                     |
   | cancel              | cancel
   v                     v
Cancelled <--------------+
```

The diagram represents allowed transitions, not a requirement to change the status twice. In addition:

- `Confirmed -> Confirmed` is allowed and idempotent.
- `Cancelled -> Cancelled` is allowed and idempotent.
- `Cancelled -> Confirmed` is rejected.
- Updating reservation details after cancellation is rejected.

### 6.2 Input requirements and database limits

The controllers enforce these requirements for creation and update:

- `RestaurantId > 0`.
- `TableId > 0`.
- Customer name is not blank and has at least **3 characters after trimming**.
- The reservation start time is UTC and **in the future** based on `DateTime.UtcNow`.
- `PartySize > 0`.

Additional enforcement comes from the engine and database:

- `DurationMinutes > 0`.
- Calculated reservation end time must be representable.
- The database limits `CustomerName` to **120 characters**.
- Database CHECK constraints require positive party size and duration, and one of the three allowed status values.

### 6.3 UTC input, storage, and legacy compatibility

- JSON and date query parameters require ISO 8601 with seconds and an explicit `Z` or offset, for example `2030-11-01T18:00:00Z` or `2030-11-01T15:00:00-03:00`. Missing-zone or invalid input returns HTTP `400`.
- Inputs normalize to UTC before business validation. Responses use UTC with `Z`; future checks use `DateTime.UtcNow`.
- The existing `datetime2` column is retained. EF requires UTC values on writes and assigns UTC kind on reads; it does not shift the stored clock values.
- Existing dates are not automatically converted. Before adopting this version on a legacy database, audit the original time zone and back up the data. Already-UTC values remain unchanged; local values need a separately reviewed, one-time conversion using the known source zone and daylight-saving ambiguity policy. Unknown-origin dates require manual review. Do not use the new UTC interpretation on an unaudited legacy database.
- Tests should use timestamps comfortably in the future and avoid exact system-clock boundaries.

### 6.4 Transactional reservation coordination

`ReservationService` depends on `IReservationWriteSession`. The real `ReservationWriteSession` delegates to the existing EF transaction/reload operations and `ReservationLock`; unit tests substitute mocks without connecting to SQL Server.

- Creation begins a transaction, locks the target table, validates, saves, and commits.
- Update begins a transaction, locks the reservation ID, loads/reloads the reservation, and locks the old and new tables before validation. Table locks are acquired in ascending ID order with duplicates removed. The reservation's own ID is excluded from overlap detection.
- Confirmation/cancellation use the reservation-ID lock, reload, and table lock before evaluating the status transition. A cancelled reservation cannot be resurrected by a stale concurrent confirmation.
- Locks use SQL Server `sp_getapplock` with exclusive mode and transaction ownership. They coordinate API instances connected to the same database and release on commit/rollback. Uncommitted early returns and failures dispose the transaction.
- Conflicting reservation writes cannot both persist overlapping active bookings when they follow this protocol. A conflicting candidate returns `ReservationConflict`, mapped to HTTP `409`. Lock timeout (10 seconds), lock cancellation/deadlock return codes, and SQL Server deadlock exceptions are also translated to HTTP `409`.
- Direct SQL and external writers that ignore the lock protocol are not protected. There is no database interval-exclusion constraint. Availability queries do not acquire these write locks.

## 7. Reservation Queries — ReservationService

| ID | Rule | Current behavior |
| --- | --- | --- |
| QRY-01 | Get-all results are ordered by reservation date ascending, then ID ascending. | Implemented |
| QRY-02 | Get by ID returns `null` when no matching record exists. | Implemented |
| QRY-03 | Restaurant and table filters return matching reservations ordered by reservation date ascending. | Implemented |
| QRY-04 | Status filtering trims input and applies equality. | Implemented |
| QRY-05 | Upcoming queries include only reservations with start time later than `DateTime.UtcNow`. | Implemented |
| QRY-06 | Upcoming confirmed/pending queries also require the specified status. | Implemented |
| QRY-07 | Search filters can be combined: status, restaurant ID, table ID, start date from/to, and minimum party size. | Implemented |
| QRY-08 | `FromDate` is inclusive (`>=`) and `ToDate` is inclusive (`<=`). | Implemented |
| QRY-09 | Search ordering can be ascending or descending by reservation date, with ID as a deterministic tie-breaker. | Implemented |
| QRY-10 | The total count is calculated **before** pagination; paging uses database-side `Skip/Take`. | Implemented |
| QRY-11 | The response contains `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, and `HasNextPage`. | Implemented |
| QRY-12 | The controller requires `PageNumber >= 1`, `PageSize` between **1 and 100**, and `FromDate <= ToDate` when both dates exist. | Controller validation |

**Pagination defaults:** `PageNumber = 1`, `PageSize = 20`.

### Reservation query test scenarios

- Combined filters only return records satisfying **all** conditions.
- Date filtering includes records exactly at both boundaries.
- Ascending and descending sorts apply the ID tie-breaker correctly.
- Page 2 returns the expected subset, while `TotalCount` reflects all filtered rows.
- An empty search result has zero items and no next page.
- Invalid pagination and reversed date range are controller validation cases unless equivalent validation is added to the service.

## 8. Database Integrity Rules

These are persisted invariants and should be verified with SQL Server integration tests where necessary, rather than being assumed to work in a mocked or EF InMemory test:

- Foreign keys link tables to restaurants and reservations to both restaurants and tables.
- Relationships use `DeleteBehavior.Restrict`.
- `(RestaurantId, Number)` is unique for tables.
- Table number is greater than zero.
- Table capacity is greater than zero and at most 50.
- Reservation party size is greater than zero.
- Reservation duration is greater than zero.
- Reservation status is one of `Pending`, `Confirmed`, or `Cancelled`.
- Reservation search indexes exist for `(Status, ReservationDate)` and `(RestaurantId, ReservationDate)`.

**Data model caveat:** Separate foreign keys for `Reservation.RestaurantId` and `Reservation.TableId` do not, by themselves, guarantee both point to the same restaurant. That relationship is currently checked by application logic during reservation create/update.

## 9. Remaining Limitations and Demonstration Data

The following limitations remain in the current implementation:

1. **Table reassignment:** Changing a table's restaurant is allowed even with linked reservations and may invalidate their restaurant/table association. A restriction would be a new business-rule change requiring a separate decision.
2. **Administrative edits:** Concurrent changes to table capacity/activity or restaurant activity do not use the reservation-write lock protocol. Reservation locks do not provide a guarantee against every administrative mutation.
3. **External writers:** SQL scripts and applications bypassing the protocol can insert conflicting intervals. Availability remains advisory until the reservation write succeeds.
4. **Legacy dates:** UTC interpretation requires the legacy-data audit described in section 6.3; the application does not infer or automatically repair old time zones.
5. **Validation boundaries:** Controller-only checks must not be assumed to exist in direct service calls. Maximum text lengths and the table-capacity upper limit are enforced by the database, without an explicit controller validation for those limits. Unit mocks cannot prove database enforcement or guarantee a friendly HTTP response for those failures.

**Seed behavior:** `scripts/seed-large.sql` inserts a positive `DurationMinutes` of **90** and generates dates using `SYSUTCDATETIME()`. On an empty migrated database it creates 10 restaurants, 80 tables, and 480 reservations with varied statuses. Rerunning it does not duplicate its identified records or rewrite their dates. Demonstration dates are relative to the initial insertion and are not refreshed on reruns. Run it administratively without concurrent application writes; the seed does not acquire the per-table reservation locks. Use the explicit `--seed` command after migrations, optionally combined as `--migrate --seed`; normal API startup does not apply migrations or seed data.

## 10. Test Reference and Execution

### Existing unit tests with mocks

The four classes in `tests/TableFlow.Tests/Unit/` use **xUnit**, **Moq**, and **MockQueryable.Moq**:

- `RestaurantServiceTests`: text trimming, update/not-found behavior, dependency checks, deletion, and propagation of unrelated persistence failures.
- `TableServiceTests`: restaurant existence, duplicate numbers, self-exclusion during update, deletion dependencies across all statuses, and persistence failures.
- `ReservationServiceTests`: engine-result mapping, creation/update, state transitions, idempotency, reload-before-decision behavior, simulated locks/transactions, commit/disposal behavior, and delegation of suggestions/availability.
- `ReservationEngineTests`: eligibility, capacity, UTC/future/party-size guards, duration, half-open overlap rules, blocking statuses, self-exclusion, suggestions, and availability.

`MockDb` provides mocked `TableFlowDbContext`/`DbSet` dependencies. `IReservationEngine`, `IReservationWriteSession`, and transactions are mocked where appropriate. These tests do not connect to SQL Server or use an EF InMemory/SQLite database. Query expressions executed against mocked collections do not establish SQL translation, collation, constraints, or concurrency correctness.

Run only these four classes:

```powershell
dotnet test --configuration Release --filter 'Category=Unit'
```

Run all unit tests, including the existing UTC parsing tests:

```powershell
dotnet test --configuration Release --filter 'Category!=Integration'
```

The last unit validation passed **86 cases** in the four mocked classes, or **93 cases** including UTC parsing tests, with no failed or skipped cases. Release avoids overwriting the Debug executable when the local API is running. Counts are a dated validation result, not a promise that every suggested scenario below already has a dedicated test.

### Existing SQL Server integration suite

The separate integration suite uses `TABLEFLOW_TEST_SQLSERVER`, creates a uniquely named disposable database, applies migrations, and cleans up only that test database. Without the variable, integration cases are explicitly skipped. It covers real HTTP reservation writes, concurrency, relationship deletions/check-delete races, UTC behavior, and migrations/seed reruns. It is not part of `Category=Unit` and was not rerun after the write-session refactor or during this documentation update; no fresh SQL Server validation is claimed here.

SQL Server-specific guarantees require that suite, not unit mocks. See `README.md` for its connection prerequisites and execution commands.

### Reference scenarios for future coverage review

### Priority 1 — Core reservation engine

Test restaurant/table eligibility, party capacity, duration boundaries, all overlap patterns, cancellation not blocking availability, self-exclusion during updates, suitable-table ordering, and availability filtering.

### Priority 2 — Reservation service

Test successful creation/update, no persistence on failed engine validation, result mapping, default `Pending` status, all allowed/forbidden status transitions, idempotent confirm/cancel, not-found results, and search/pagination.

### Priority 3 — Table and restaurant services

Test CRUD behavior, normalization, existence checks, duplicate table numbers, active filters, sorting, and deletion behavior.

### Priority 4 — Integration tests for persistence guarantees

Verify SQL Server CHECK constraints, unique indexes, foreign keys/restricted deletion, and concurrent creation/update conflicts using the implemented locking strategy. Do not infer complete constraint coverage merely because migrations and the seed execute successfully.

### Test implementation guidelines

- Use **xUnit** and mocks for service-unit tests; isolate `DbContext`/`DbSet`, `IReservationEngine`, and `IReservationWriteSession` as needed.
- Preserve the distinction between mocked decisions and real SQL behavior. Use SQL Server integration tests to validate query translation, collation, constraints, transactions, and concurrency. Do **not** treat mocked queries, SQLite, or EF Core's nonrelational InMemory provider as equivalent evidence.
- Use clearly named **Arrange / Act / Assert** tests that express the business outcome, not implementation details.
- For controller-only input validation, either add focused controller tests or explicitly leave it outside the service-unit-test suite. Do not write service tests expecting validation that only exists in a controller.
- **Do not add Docker.** Assume the developer already has a local database environment and document any prerequisites for optional integration tests.
- Do not change existing business rules merely to make a test pass. If the local code differs from this document, report the discrepancy and follow the confirmed local business contract.

## 11. Review and Validation Status

- [x] Current local implementation compared with this document.
- [x] Mocked unit tests exist for all four requested components, including successful and negative/boundary scenarios.
- [x] Latest unit validation: 86 mocked cases passed; 93 passed including UTC parsing tests. Release build had no warnings/errors and formatting was verified.
- [x] SQL Server integration evidence is distinguished from unit evidence; no post-refactor integration rerun is claimed.
- [x] Remaining reassignment, administrative-concurrency, external-writer, legacy-date, and validation-boundary limitations are documented.
- [x] No Docker, unrelated features, or additional API changes are introduced by this documentation update.
