# Developer Journal

Technical notes, implementation decisions, trade-offs, and project evolution for TableFlow.

---

## Phase 1 — Sprint 1 — Mission 1

### Implementation Summary

Implemented the first REST endpoints for the Restaurants module:

- GET /restaurants
- GET /restaurants/{id}

Created the initial response DTO:

- RestaurantResponse

The first version used in-memory data to keep the focus on the API contract.

### Technical Decisions

Restaurant data was temporarily stored in memory.

The API exposed restaurants as a REST resource using the route:

- /restaurants

The controller returned appropriate HTTP responses:

- 200 OK for successful requests
- 400 Bad Request for invalid input
- 404 Not Found when the restaurant was not found

### Rationale

The goal of this step was to define a clear and predictable HTTP contract before introducing persistence, services, or additional architecture.

Starting with in-memory data kept the implementation simple and allowed the API behavior to be validated first.

### Trade-offs

Pros:

- Simple implementation
- Clear focus on HTTP and REST
- Easy to test manually
- No infrastructure complexity

Cons:

- Data is not persisted
- Not production-ready
- Controller temporarily contains too much responsibility

### Validation

Tested the following scenarios:

- GET /restaurants returns 200 OK
- GET /restaurants/{id} returns 200 OK when the restaurant exists
- GET /restaurants/0 returns 400 Bad Request
- GET /restaurants/999 returns 404 Not Found

### Next Steps

Move restaurant retrieval logic out of the controller and introduce a simple service layer.

---

## Phase 1 — Sprint 1 — Mission 2

### Implementation Summary

Moved restaurant retrieval logic from the controller to a service layer.

Created:

- IRestaurantService
- RestaurantService

Updated the RestaurantsController to use Dependency Injection.

Implemented and preserved the following endpoints:

- GET /restaurants
- GET /restaurants/{id}
- GET /restaurants/city/{city}
- GET /restaurants/cuisine/{cuisineType}
- GET /restaurants/active

### Technical Decisions

Introduced a simple service layer inside the API project.

The controller is responsible for HTTP concerns:

- Route parameters
- Basic validation
- Status codes
- HTTP responses

The service is responsible for application logic:

- Retrieving restaurants
- Filtering by city
- Filtering by cuisine type
- Filtering active restaurants

The controller depends on the IRestaurantService abstraction instead of directly depending on the RestaurantService implementation.

### Rationale

The Restaurants module now has multiple operations, so keeping all logic inside the controller would make it harder to maintain.

Introducing a service layer keeps the controller thinner and improves separation of concerns.

This is not a full architecture split yet. It is a simple modular organization inside the API project.

### Trade-offs

Pros:

- Cleaner controller
- Better separation of concerns
- More maintainable code
- Easier to evolve toward persistence later
- Prepares the code for future testing

Cons:

- More files
- Slightly more structure for a small feature
- Data is still stored in memory

### Validation

Tested the following scenarios:

- GET /restaurants returns 200 OK
- GET /restaurants/{id} returns 200 OK when found
- GET /restaurants/0 returns 400 Bad Request
- GET /restaurants/999 returns 404 Not Found
- GET /restaurants/city/{city} returns matching restaurants
- GET /restaurants/city returns 400 Bad Request
- GET /restaurants/cuisine/{cuisineType} returns matching restaurants
- GET /restaurants/cuisine returns 400 Bad Request
- GET /restaurants/active returns active restaurants

### Notes

Adjusted route and parameter naming to keep the API language consistent:

- cuisine/{cuisineType}
- GetByCuisineType
- CuisineType
- cuisineType

This improves readability and keeps the code aligned with the domain language.

### Next Steps

Add support for creating restaurants through the API.

---

## Phase 1 — Sprint 1 — Mission 3

### Implementation Summary

Added support for creating restaurants.

Implemented:

- POST /restaurants
- CreateRestaurantRequest
- Create method in IRestaurantService
- Create logic in RestaurantService

The API now receives restaurant data from the request body and adds a new restaurant to the in-memory list.

### Technical Decisions

Created a dedicated request DTO for restaurant creation:

- CreateRestaurantRequest

Kept the response DTO separate:

- RestaurantResponse

The controller validates the input before calling the service.

The service is responsible for generating a temporary ID, creating the restaurant object, adding it to the in-memory list, and returning the created restaurant.

The endpoint returns:

- 201 Created when the restaurant is created successfully

### Rationale

POST is the appropriate HTTP method for creating a new resource.

Using a dedicated request DTO keeps the input contract explicit and separate from the response contract.

Returning 201 Created is more precise than returning 200 OK because the request creates a new resource.

### Trade-offs

Pros:

- Clear creation endpoint
- Explicit request and response contracts
- Controller remains focused on HTTP concerns
- Service handles creation logic
- The module now supports the first write operation

Cons:

- Data is still stored in memory
- ID generation is manual
- Validation is still basic and manual
- No database persistence yet

### Validation

Tested the following scenarios:

- POST /restaurants returns 201 Created when the request is valid
- Created restaurant can be retrieved with GET /restaurants/{id}
- Created restaurant appears in GET /restaurants
- Empty name returns 400 Bad Request
- Empty cuisine type returns 400 Bad Request
- Empty city returns 400 Bad Request
- Name with fewer than 3 characters returns 400 Bad Request

### Technical Notes

Request DTOs represent data coming into the API.

Response DTOs represent data going out of the API.

For example, the client does not send the restaurant ID when creating a restaurant, but the API returns the generated ID in the response.

### Next Steps

Add support for updating existing restaurants.

---

## Phase 1 — Sprint 1 — Mission 4

### Implementation Summary

Added support for updating existing restaurants.

Implemented:

- PUT /restaurants/{id}
- UpdateRestaurantRequest
- Update method in IRestaurantService
- Update logic in RestaurantService

The API now receives the restaurant ID from the route and the updated data from the request body.

### Technical Decisions

Created a dedicated request DTO for updates:

- UpdateRestaurantRequest

The controller validates:

- Restaurant ID
- Name
- Cuisine type
- City

The service is responsible for locating the restaurant in the in-memory list and replacing it with the updated version.

The endpoint returns:

- 200 OK when the restaurant is updated successfully
- 400 Bad Request when the request is invalid
- 404 Not Found when the restaurant does not exist

### Rationale

PUT is the appropriate HTTP method for updating an existing resource.

The ID is provided in the route because it identifies which restaurant should be updated.

The request body contains the new state of the resource.

Returning 200 OK with the updated restaurant makes the response clear during this stage of development.

### Trade-offs

Pros:

- Clear update contract
- Controller remains focused on HTTP concerns
- Service centralizes update logic
- The Restaurants module moves closer to a complete CRUD flow

Cons:

- Data is still stored in memory
- Validation is still manual
- No database persistence yet
- The update operation replaces the full restaurant object

### Validation

Tested the following scenarios:

- PUT /restaurants/{id} returns 200 OK when the restaurant exists
- Updated restaurant can be retrieved with GET /restaurants/{id}
- PUT /restaurants/0 returns 400 Bad Request
- PUT /restaurants/999 returns 404 Not Found
- Empty name returns 400 Bad Request
- Name with fewer than 3 characters returns 400 Bad Request
- Empty cuisine type returns 400 Bad Request
- Cuisine type with fewer than 3 characters returns 400 Bad Request
- Empty city returns 400 Bad Request

### Technical Notes

POST is used to create a new resource.

PUT is used to update an existing resource.

201 Created is appropriate for creation.

200 OK is appropriate here because the update succeeds and the API returns the updated resource.

### Next Steps

Add support for deleting restaurants and continue improving validation consistency across the module.