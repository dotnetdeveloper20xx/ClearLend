# ClearLend User Progress — Borrower Foundation

Owner: Faz Ahmed

## What the user can do today

The system can accept a person’s email address, name, and consent information.

It creates:

- A user account.
- A borrower profile linked to that user account.

The information is now saved permanently in the local SQL Server database instead of disappearing when the application stops.

The API is available through documented controller endpoints, and developers can inspect and test those endpoints through Swagger.

The Angular application also contains the planned registration, login, and profile pages.

## Faz Ahmed borrower journey

### Completed

- Faz can submit an email address, password, given name, family name, and consent choice.
- The API validates the registration details.
- ASP.NET Core Identity securely hashes and stores the password.
- SQL Server stores the Identity account.
- SQL Server stores the ClearLend user account and borrower profile.
- The Identity account is linked to the ClearLend domain user account.
- Duplicate email addresses are rejected.
- The API is available through Swagger for developer verification.

### Remaining

- Verify Faz’s email and password during login.
- Issue a signed JWT after successful login.
- Configure the API to validate JWT bearer tokens.
- Use the authenticated JWT to identify Faz’s account.
- Return only Faz’s borrower profile from `GET /api/v1/me/borrower-profile`.
- Connect the Angular login page to the live login endpoint.
- Connect the Angular profile page to the protected profile endpoint.
- Test the complete journey from registration through login to profile viewing.

## What is not available yet

The complete borrower journey is not finished. A borrower can now submit a password during registration, and the system stores it through ASP.NET Core Identity. A borrower still cannot yet:

- Log in successfully.
- Receive a JWT access token.
- Open a protected profile using that token.

## Next user-facing outcome

The next development step is to connect JWT authentication to the Identity account so a borrower can:

1. Register with a password.
2. Log in with email and password.
3. Receive a secure access token.
4. View their own borrower profile.
