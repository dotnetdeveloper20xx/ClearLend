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

## What is not available yet

The complete borrower journey is not finished. A borrower cannot yet:

- Create a password.
- Log in successfully.
- Receive a JWT access token.
- Open a protected profile using that token.

## Next user-facing outcome

The next development step is to connect ASP.NET Core Identity and JWT authentication so a borrower can:

1. Register with a password.
2. Log in with email and password.
3. Receive a secure access token.
4. View their own borrower profile.
