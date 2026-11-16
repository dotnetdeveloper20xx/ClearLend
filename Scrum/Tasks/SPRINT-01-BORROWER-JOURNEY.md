# Sprint 1 — Basic borrower journey

Owner: Faz Ahmed  
Outcome: Completed at the end of day 1

## Product owner notes

We agreed to start with one small, complete user journey instead of building the whole lending platform at once.

The first useful outcome was for Faz Ahmed to:

1. Create a borrower account.
2. Sign in with the email address and password.
3. See his own borrower profile.

This was chosen because it proves the most important foundation of the product: a real person can enter the system, be securely identified, and see information belonging only to that person.

We deliberately left lenders, loans, applications, vetting, payments, dashboards, email verification, and password reset for later sprints.

## Developer notes

The completed journey is:

```text
Angular registration page
    → controller-based API
    → CQRS command and validator
    → ASP.NET Core Identity
    → SQL Server
    → JWT login
    → protected profile query
    → Angular borrower profile page
```

The implementation includes:

- Controller-based ASP.NET Core API.
- Clean Architecture boundaries.
- CQRS commands, queries, handlers, and validators.
- EF Core SQL Server persistence.
- ASP.NET Core Identity for password hashing and verification.
- JWT access-token creation and bearer authentication.
- Domain `UserAccount` linked to the Identity subject.
- `BorrowerProfile` linked to the domain user account.
- Angular registration, login, and profile pages.
- Angular JWT interceptor and session token storage.
- Swagger/OpenAPI documentation.
- Visual Studio launch configuration for the API and Angular application.
- Local SQL Server configuration and EF Core migrations.

## End-of-day result

Faz successfully registered and logged in. The profile page displayed:

- Faz Ahmed’s name.
- Faz’s email address.
- Account status.
- Borrower profile status.
- Consent status.
- Registration date.

This proves the basic borrower registration, login, and profile journey is working as a complete vertical slice.

## Known issue

The profile displayed the registration date as `Dec 31, 1`, which indicates the date is currently being saved or mapped as the .NET default date. This is a small data-mapping defect to fix next.

The user interface also needs more polished handling for expired sessions and API errors before this is considered production-ready.

## Not included in this sprint

- Email verification.
- Password reset.
- Multi-factor authentication.
- Lender registration.
- Loan applications.
- Vetting workflows.
- Payments.
- Dashboards.
- Production hosting and deployment.

## Next recommended work

Fix and test the registration date, then add a complete automated test for:

```text
register → login → authenticated profile request
```

Only after that should the team begin the next user journey, such as lender registration.
