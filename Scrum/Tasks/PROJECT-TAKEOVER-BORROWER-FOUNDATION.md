# ClearLend Project Takeover — Borrower Foundation

Owner: Faz Ahmed  
Purpose: Record the business requirements and developer work completed after taking over the existing repository.

## Business requirement

A person must be able to become a borrower in ClearLend. The system should store the borrower information safely, and the borrower will later be able to create a password, log in, and view their own profile.

We are starting with this small working journey before building lending, credit, staff, vetting, or dashboard features.

## What we found when taking over

- The repository already contained domain, application, infrastructure, API, tests, and an Angular project.
- Several older features were designed in advance but were not fully connected to working repositories or services.
- The API originally used minimal API routes, although the project is better suited to controller-based APIs as it grows.
- User and borrower data was initially held in memory, so it disappeared when the application stopped.
- Login and profile CQRS files existed or were partly prepared, but real Identity and JWT services were not yet connected.

## Decisions made

- Use Clean Architecture boundaries already present in the solution.
- Use controllers for the API.
- Use CQRS through MediatR for commands and queries.
- Use SQL Server locally through EF Core.
- Use ASP.NET Core Identity for password storage and account login in the next slice.
- Use JWT for API authentication after Identity registration is complete.
- Keep Angular as the single SPA, with only registration, login, and profile pages for now.
- Keep future staff, vetting, work-item, and dashboard functionality out of the current working slice.

## Developer tasks completed

### 1. API controller foundation

- Replaced minimal API route definitions with MVC controllers.
- Added controllers for users, borrowers, authentication, the current borrower profile, and system status.
- Kept controllers thin: they send commands or queries and translate results into HTTP responses.
- Added Swagger/OpenAPI documentation and response metadata.

### 2. Borrower registration flow

- Kept the existing borrower registration command, validator, handler, domain rules, and repository abstractions.
- The registration flow creates a user account and borrower profile.
- Validation and duplicate-email handling remain in the application layer.

### 3. EF Core and SQL persistence

- Added `ClearLendDbContext`.
- Added SQL repositories for user accounts and borrower profiles.
- Added an EF Core unit of work.
- Mapped IDs and value objects used by the current domain.
- Mapped borrower consent as an owned value object.
- Added EF-only constructors so EF Core can safely materialize domain aggregates.
- Added an EF design-time factory for migration generation.
- Added and applied the initial migration to the local SQL Server database.

### 4. Focused application startup

- The application currently activates only the borrower registration, login, and profile handlers.
- Older staff, vetting, and work-item handlers remain in the repository but are not activated because their repositories are not ready.
- This allows the current feature to start and be tested without pretending unfinished features are production-ready.

### 5. Local database configuration

- Configured the local development connection for the SQL Server instance shown in SSMS:

```text
Server=.
Database=ClearLend
Windows Authentication
```

- Migration `InitialCreate` was applied successfully.

## Current result

The repository now has a working foundation for storing users and borrower profiles in local SQL Server. The API is controller-based, follows the existing CQRS and Clean Architecture boundaries, and builds successfully.

## Not yet completed

- ASP.NET Core Identity user and password persistence.
- Borrower password registration.
- Real login verification.
- JWT token creation and validation.
- Authentication protection on the profile endpoint.
- End-to-end registration, login, and profile integration tests.
- Email verification and password reset.

## Definition of done for the next developer task

The next task is complete when a borrower can:

1. Register with email, name, consent, and password.
2. Have the account and password stored securely in SQL Server.
3. Log in with email and password.
4. Receive a valid JWT.
5. Use the JWT to retrieve only their own borrower profile.
6. Demonstrate the complete journey through Swagger and Angular.

## Verification completed

- API Release build: passed with zero warnings and zero errors.
- EF Core migration generation: passed.
- EF Core database update: passed against local SQL Server.
- Angular build and tests: previously passed; Angular remains the single SPA in the repository.
