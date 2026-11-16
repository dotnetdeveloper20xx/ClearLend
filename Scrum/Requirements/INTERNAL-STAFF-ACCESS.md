# ClearLend Internal Staff Access

## Business requirements

### Purpose

ClearLend needs trusted internal users who can operate the lending platform and support borrowers and lenders. These users must be separate from public borrower and lender registration.

### User groups

#### External users

- **Borrower**: creates a profile, submits funding requests, and reviews agreements.
- **Lender / Investor**: creates a business profile, reviews suitable opportunities, and manages funding activity.

#### Internal users

- **Application Owner**: owns the platform configuration and has full administrative responsibility.
- **Operations Staff**: manages day-to-day application processing, documents, communication, and operational statuses.
- **Credit Manager**: reviews credit information and makes or approves credit-limit decisions.

### Account and access principles

1. Internal staff must not register through the public borrower or lender forms.
2. Staff accounts must be created manually by an authorized Application Owner.
3. Every staff member must have an identified role.
4. Access must be controlled by permissions, not only by frontend navigation.
5. Sensitive actions must be protected by backend authorization.
6. Staff actions that affect applications, credit decisions, users, or settings must be auditable.
7. The initial Application Owner must be provisioned securely and must not be publicly self-registered.
8. A user must not accidentally receive both a borrower/lender external profile and an unrelated staff identity without an explicit business decision.

### Initial role permissions

| Capability | Application Owner | Operations Staff | Credit Manager |
|---|---:|---:|---:|
| View platform dashboard | Yes | Yes | Yes |
| View borrower and lender records | Yes | Yes | Yes, subject to credit scope |
| Manage application workflow | Yes | Yes | Review only |
| Manage documents and operational notes | Yes | Yes | Read/review as needed |
| Review credit information | Yes | Yes, if assigned | Yes |
| Approve or change credit limits | Yes | No | Yes |
| Add, disable, and manage staff | Yes | No | No |
| Manage roles and permissions | Yes | No | No |
| Manage platform settings | Yes | No | No |
| View audit history | Yes | Operational scope | Credit scope |

These permissions are the initial business baseline and must be refined before production use.

### Staff lifecycle

The first release will use administrator-controlled staff creation. There will be no invitation flow and staff will not register themselves.

1. Application Owner is provisioned.
2. Owner manually creates a staff account using the staff member's email address and assigns one or more roles.
3. The system creates the account with an administrator-issued temporary password and marks it as requiring a password change.
4. Staff member signs in with the temporary password and must set a private password before accessing staff functions.
5. Staff account becomes active after the password is established.
6. Owner can disable access without deleting the account or audit history.
7. Role changes take effect on the next authorization check or token refresh.

The Application Owner creates the account and may provide the temporary password through the approved internal process, but must not be able to view or recover the staff member's permanent password.

### Staff management area

Application Owners need a protected staff-management area to administer internal users. This area must be available only to authorized Application Owners and must enforce the same permissions through the API.

The first release should support:

- view staff members and their current status;
- search and filter by name, email, role, and status;
- add a staff member manually;
- assign one or more roles;
- change assigned roles;
- block or suspend access immediately;
- restore a suspended or blocked account when appropriate;
- reset a staff member's password through a controlled process;
- force a password change at next login;
- resend or display a controlled first-time access action where applicable;
- send operational email, such as password-reset or account-status notifications;
- view when the account was created, changed, suspended, restored, or removed;
- view the relevant audit history;
- remove a staff member from active staff access without deleting historical records.

For business integrity, removal should normally be a soft removal or permanent disabled state. It must not delete application reviews, credit decisions, notes, audit events, or other records created by that staff member. Permanent data deletion should be a separate, restricted process and is not part of the first release.

Every management action must require appropriate authorization, provide a clear confirmation for destructive or disruptive actions, and create an audit event containing the acting owner, affected staff account, action, timestamp, and relevant reason or metadata.

### Business acceptance criteria

- A public visitor cannot create an internal staff account.
- Only an authorized Application Owner can create a staff account or assign staff roles.
- Staff members cannot invite themselves, register themselves, or select their own roles.
- A staff member can only access functions allowed by their assigned permissions.
- A Credit Manager can review and approve credit limits but cannot manage platform settings.
- Operations Staff can progress applications and manage operational information but cannot approve credit limits.
- The Application Owner can manage staff access and platform configuration.
- Disabled staff cannot access protected staff endpoints.
- Staff actions record who acted, what changed, when it changed, and the affected record.
- Borrower and lender journeys continue to work unchanged.

## Developer tasks

### 1. Domain model

- [ ] Define `StaffProfile` or `StaffMembership` and its lifecycle states.
- [ ] Define role identifiers: `ApplicationOwner`, `OperationsStaff`, and `CreditManager`.
- [ ] Define permission identifiers separately from role names.
- [ ] Add domain rules for owner-controlled staff creation and disabling access.
- [ ] Decide whether one user can hold multiple staff roles.
- [ ] Decide whether a staff account may also have a borrower or lender profile.
- [ ] Add domain tests for valid and invalid lifecycle transitions.

### 2. Persistence and migrations

- [ ] Add tables for staff memberships, roles, permissions, and staff lifecycle state as required by the final design.
- [ ] Add a unique constraint for the intended staff identity relationship.
- [ ] Add indexes for email, status, and lifecycle timestamps where appropriate.
- [ ] Add foreign keys and delete behavior deliberately; do not rely on application checks alone.
- [ ] Generate EF migrations from the verified model.
- [ ] Verify the migration against a clean database and an existing development database.
- [ ] Do not suppress pending-model warnings to hide migration drift.
- [ ] Add persistence integration tests.

### 3. Identity and security

- [ ] Reuse the existing Identity account only where that matches the final account model.
- [ ] Implement Application Owner-only staff creation and role assignment.
- [ ] Implement a temporary-password state and forced password change on first staff login.
- [ ] Ensure the owner cannot see or retrieve a staff member's permanent password.
- [ ] Do not implement public staff registration or staff self-selected roles.
- [ ] Enforce password and account-status rules on the backend.
- [ ] Add role and permission claims or server-side permission checks.
- [ ] Ensure disabled users cannot call staff APIs with an old token.
- [ ] Keep demo password rules separate from production password rules.
- [ ] Add authorization tests for every staff role.

### 4. Application layer

- [ ] Add commands and handlers for Application Owner-controlled staff creation.
- [ ] Add commands and handlers for first-login password change and controlled password reset.
- [ ] Add commands and handlers for disabling staff access.
- [ ] Add commands and handlers for assigning and removing roles.
- [ ] Add queries for staff lists, current staff profile, and effective permissions.
- [ ] Add validation for email, password setup state, role selection, and authorization.
- [ ] Make staff account creation and membership creation transactional.
- [ ] Add unit tests for success, duplicate invitation, expired invitation, unauthorized action, and disabled account cases.

### 5. API layer

- [ ] Add protected staff endpoints under a clear route such as `/api/v1/staff`.
- [ ] Add owner-only endpoints for staff creation and role management.
- [ ] Add operations endpoints for application and document workflow.
- [ ] Add credit-manager endpoints for credit review and limit decisions.
- [ ] Return consistent problem/error responses.
- [ ] Confirm authorization is enforced in the API, not only in Angular guards.
- [ ] Add API integration tests for allowed and denied requests.

### 6. Frontend

- [ ] Add a staff first-password setup or password-reset page.
- [ ] Add a staff login/profile experience where necessary.
- [ ] Add a role-aware staff dashboard.
- [ ] Show only navigation options allowed by the current permissions.
- [ ] Add loading, empty, success, validation, and API error states to every staff form.
- [ ] Make destructive actions explicit and confirmable.
- [ ] Use the existing ClearLend landing-page design language.
- [ ] Make pages responsive and keyboard accessible.
- [ ] Add route guards for staff areas, while keeping backend authorization authoritative.
- [ ] Add frontend tests for role-based navigation and form submission.

### 7. Audit and operational controls

- [ ] Define the audit event structure before implementing staff actions.
- [ ] Record actor, role, action, target record, timestamp, and meaningful before/after values.
- [ ] Prevent sensitive credentials and tokens from being written to logs.
- [ ] Add an owner-facing audit history view or API query.
- [ ] Decide retention and export requirements.

### 8. Staff management area

- [ ] Add an Application Owner-only staff-management route and dashboard.
- [ ] Add staff list, search, filtering, status, and role displays.
- [ ] Add manual staff creation with validation and duplicate-account handling.
- [ ] Add role assignment and role-change workflows.
- [ ] Add block, suspend, restore, and soft-remove workflows.
- [ ] Add controlled password-reset and forced-password-change workflows.
- [ ] Add email actions with a provider abstraction rather than direct provider calls in controllers.
- [ ] Add confirmation dialogs and reason capture for suspension, blocking, removal, and role changes.
- [ ] Ensure management actions are disabled or rejected when the current user lacks permission.
- [ ] Add backend authorization tests for every management action.
- [ ] Add frontend tests for list states, filters, forms, confirmations, success, and failure responses.

### 9. Verification checklist

- [ ] Domain build passes.
- [ ] Application build passes.
- [ ] Infrastructure build passes.
- [ ] API build passes.
- [ ] Angular build passes with no errors.
- [ ] Automated tests pass.
- [ ] EF migration has no pending model changes.
- [ ] Owner invitation flow works end to end.
- [ ] Operations Staff authorization is correct.
- [ ] Credit Manager authorization is correct.
- [ ] Disabled staff access is rejected.
- [ ] Borrower registration, login, and profile still work.
- [ ] Lender registration, login, and profile still work.
- [ ] Responsive and visual review is complete.

## Open decisions before implementation

1. Is ClearLend supporting one internal operating team initially, or multiple lender organizations with separate staff teams?
2. Can one person hold multiple staff roles?
3. Can one email address have both an external profile and an internal staff membership?
4. Should Operations Staff see all applications or only assigned applications?
5. Can Credit Managers approve their own reviews, or is a second approval required?
6. What data may each role view, especially personal and financial information?
7. What is the required audit retention period?
8. Should staff use the same login screen as borrowers and lenders, or a separate staff sign-in route?
9. Should the owner receive a one-time temporary credential, or should the system provide a controlled password-reset action?

## Recommended first release scope

For the first implementation, build one internal ClearLend team with:

- one provisioned Application Owner;
- Application Owner-controlled staff onboarding;
- Operations Staff and Credit Manager roles;
- backend-enforced permissions;
- a basic staff dashboard;
- audit records for staff actions;
- no public staff registration or invitation flow;
- no multi-organization tenancy yet.

Multi-organization support, advanced approval workflows, fine-grained data masking, and delegated administration should remain later design work until the first staff workflow is proven.

## Engineering operating prompt

Use the following prompt when implementing this feature. It is intended to protect the product owner from avoidable regressions, hidden assumptions, incomplete flows, and premature claims of completion.

> Implement the ClearLend internal staff access feature as a senior engineer responsible for a production-quality result. Work carefully and deliberately. Do not rush to code, do not guess about existing behavior, and do not declare success because one project compiles.
>
> First inspect the complete repository and current working tree. Review the existing domain model, application handlers, repositories, EF mappings, migrations, Identity configuration, JWT flow, API controllers, Angular routes, forms, shared styles, tests, launch configuration, and previously implemented borrower and lender journeys. Identify existing conventions and risks before making changes.
>
> Before implementation, write down and validate the proposed design: user journeys, role definitions, permission matrix, account relationship, invitation lifecycle, authorization boundaries, API endpoints, request and response contracts, database entities, migration plan, frontend routes, screen states, and test cases. Clearly distinguish confirmed facts from assumptions. Do not proceed with a materially ambiguous business rule; raise it for decision.
>
> Treat the feature as one end-to-end system. Keep the domain rules, database schema, API contracts, authentication, authorization, frontend navigation, forms, and displayed data consistent. Any property persisted in the database must be checked for EF discoverability, nullability, column type, migration representation, insert behavior, update behavior, and materialization. Generate migrations from the verified model. Never manually edit migration metadata or suppress migration warnings merely to make startup continue.
>
> Implement in small layers and validate each layer before moving forward: domain, application, persistence, migrations, API, authentication and authorization, frontend models and services, routes and guards, screens and styles, then automated tests. Keep code readable and typed. Do not use `any`, unexplained magic strings, compressed code that prevents review, or duplicated role logic in multiple places.
>
> Enforce security on the backend. Angular guards and hidden menu items are usability features, not security controls. Test every staff permission with both an allowed request and a denied request. Verify invitation tokens are single-use, time-limited, protected from disclosure, and never logged. Verify disabled staff cannot continue using protected endpoints. Verify sensitive data is not exposed to roles that should not see it.
>
> For every frontend screen and form, review the complete experience: visual hierarchy, spacing, typography, colors, field labels, required indicators, hints, validation messages, keyboard access, focus behavior, loading state, disabled state, duplicate-submit protection, success state, API failure state, empty state, responsive layout, refresh behavior, and navigation after success. Use the ClearLend landing page as the visual standard. Do not leave raw prototype layouts or browser-default controls in a finished feature.
>
> Test the complete workflows, not isolated functions:
>
> - provision or invite an Application Owner;
> - invite an Operations Staff member;
> - invite a Credit Manager;
> - accept an invitation;
> - reject an expired or reused invitation;
> - sign in with each role;
> - access permitted pages and API endpoints;
> - reject forbidden pages and API endpoints;
> - disable a staff account and verify access is rejected;
> - change roles and verify permissions change;
> - record and inspect audit events;
> - verify borrower registration, login, and profile still work;
> - verify lender registration, login, and profile still work.
>
> Before requesting manual testing, run and record the evidence for every available check: domain build, application build, infrastructure build, API build, Angular build, automated tests, migration validation, API contract review, authorization tests, and frontend tests. If any check cannot run because of the environment, state the exact reason and do not describe the result as passed.
>
> Perform a final code review focused on failure modes: partial account creation, duplicate email, duplicate invitation, stale tokens, missing database rows, null values, incorrect role routing, incorrect status transitions, unauthorized access, inconsistent error responses, broken refresh behavior, and responsive layout failures. Review the diff for accidental changes and confirm the working tree contains only intended work.
>
> Report completion in four sections: verified, not verified, known risks, and manual test instructions. Do not commit, push, start, stop, or reset anything unless explicitly requested. Do not claim the feature is complete until the evidence supports that claim.

## Build, runtime, and regression verification standard

The implementation is not ready for product-owner testing until both applications and the existing journeys have been checked.

### Backend verification sequence

1. Restore dependencies.
2. Build Domain.
3. Build Application.
4. Build Infrastructure and inspect EF mappings.
5. Generate or validate migrations and confirm there are no pending model changes.
6. Build the API.
7. Run unit, architecture, and integration tests.
8. Start the API using the configured Visual Studio profile.
9. Verify `/health/live`, `/health/ready`, and Swagger.
10. Exercise borrower, lender, and staff API journeys.

### Frontend verification sequence

1. Restore npm dependencies.
2. Run the Angular production build and type checking.
3. Run the Angular test suite.
4. Verify every route, component import, and lazy-loaded feature.
5. Verify each service method against the backend HTTP method, URL, request model, and response model.
6. Start Angular through the configured Visual Studio/npm profile.
7. Exercise registration, login, role-based routing, profile loading, logout, validation, loading, empty, and API-error states.

### Regression gate

The following existing journeys must be rechecked after staff functionality is added:

- borrower registration;
- borrower login and borrower profile routing;
- lender registration;
- lender login and lender profile routing;
- duplicate-email handling;
- invalid forms making no API request;
- protected routes rejecting unauthenticated users;
- old tokens and logout behavior;
- existing API endpoints and database migrations.

Compilation alone is not sufficient. Report these separately:

```text
Compiles: passed or failed
Automated tests: passed, failed, or unavailable
Database/migrations: verified or not verified
End-to-end flows: verified or not verified
Visual review: completed or not completed
Environment blockers: exact cause
```

## Angular state, route, API, and UI review standard

Before declaring the staff feature complete, review the Angular application for:

- whether local signals, service state, or NgRx is the appropriate choice for each feature;
- typed actions, reducers, selectors, effects, and feature state where NgRx is used;
- no unnecessary mixing of NgRx and signals;
- loading, success, empty, error, refresh, logout, and stale-data behavior;
- duplicate request prevention and request cancellation where appropriate;
- safe handling of authentication state and sensitive data;
- every link having a valid destination;
- every button invoking the intended action;
- every form using the correct submit event;
- no duplicate submissions;
- every API call using the correct HTTP method and URL;
- request and response models matching the backend contracts;
- borrower, lender, and staff routes not crossing;
- route guards being supplemented by backend authorization;
- responsive, keyboard-accessible, and visually consistent layouts;
- clear field labels, required indicators, hints, validation messages, loading states, disabled states, empty states, and failure messages;
- no placeholder links, dead routes, raw browser-default layouts, misleading wording, or incomplete screens.

For each important user action, document and verify the complete chain:

```text
UI action
→ Angular route/component
→ service/state method
→ HTTP method and URL
→ API controller
→ application handler
→ database operation
→ response contract
→ rendered result
```

NgRx must not be introduced merely because the package is installed. The chosen state approach must be justified by the feature’s complexity, tested, typed, and consistent with the rest of the application.
