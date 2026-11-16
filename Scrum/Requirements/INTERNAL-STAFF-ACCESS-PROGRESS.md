# Internal Staff Access — Progress Record

This file records implementation progress so the feature direction is not lost between sessions.

## Agreed scope

- One internal ClearLend team for the first release.
- Application Owner manually creates staff accounts.
- No public staff registration.
- No invitation email flow in the first release.
- Real email delivery is deferred; email operations are mocked and recorded internally for now.
- One person may hold multiple staff roles.
- Staff use the existing ClearLend login identity.
- Staff access is separate from borrower and lender profiles.
- Staff accounts are created by the Application Owner with a temporary password and must change it on first login.
- Removing staff means removing access while preserving historical records.

## Status

### Completed

- [x] Requirements documented.
- [x] Staff roles defined.
- [x] Staff membership domain model added.
- [x] Active/disabled membership state added.
- [x] Staff membership restore behavior added at domain level.
- [x] Domain validation added.
- [x] Initial domain tests added.
- [x] Staff membership repository abstraction added.
- [x] EF staff membership mapping added.
- [x] Staff repository registration added.

### In progress

- [ ] Align the requirements document with the manual-account-creation decision.
- [ ] Define and implement staff account lifecycle and forced first-login password change.
- [x] Implement the first Application Owner authorization check.
- [x] Implement the initial owner-controlled staff creation command and validator.
- [x] Add the initial staff creation API endpoint.
- [x] Add Angular staff-list and staff-create API models and client methods.
- [x] Include staff accounts in login account-type classification.
- [x] Add temporary-password flag to Identity accounts.
- [x] Add forced password-change API contract and endpoint.
- [x] Add Angular API method for password change.
- [x] Add forced-password-change route and screen.
- [x] Implement initial owner-only staff list query and API endpoint.
- [x] Add initial Angular staff dashboard screen for the staff list.
- [ ] Implement staff management application commands and queries for lifecycle actions.
- [x] Add owner-authorized staff suspend/restore command and API endpoint.
- [x] Add Application Owner staff password-reset command and API endpoint.
- [x] Add Application Owner staff role-change command and API endpoint.
- [x] Add permanent staff removal semantics and owner-only API endpoint while preserving membership history.
- [ ] Block all protected staff access until the password change is completed.
- [x] Angular authenticated routes redirect temporary-password users to the password-change screen.
- [x] Add development-only idempotent Application Owner startup seeding from configuration.
- [x] Deny suspended and removed staff accounts during login before issuing an access token.
- [x] Generate the initial staff-membership database migration.
- [ ] Apply and verify the migration against the development database.
- [ ] Add staff management API endpoints.
- [x] Add audit record domain model, repository, EF mapping, DI registration, and staff-creation audit.
- [x] Wire audit records for suspend, restore, role changes, removal, and password reset.
- [x] Generate the audit-table migration.
- [x] Apply and verify the staff/audit migrations against the development database.
- [x] Add Angular staff dashboard create form with validation and API submission wiring.
- [ ] Verify staff create-form submission end to end against the running API.

### Not yet verified

- [ ] Command-line Infrastructure/API build verification; Visual Studio solution rebuild has succeeded (11/11 projects).
- [ ] Automated .NET test execution; the local test host currently hangs after discovery.
- [ ] EF migration application.
- [ ] Staff end-to-end workflow.
- [ ] Borrower and lender regression workflow after staff changes.

## Current risks

1. The local .NET test host hangs after discovering the test assembly.
2. The local SDK/workload environment has previously stalled Infrastructure builds.
3. Staff management must not rely on Angular guards; API authorization is required.
4. Temporary staff passwords must not expose permanent passwords or allow staff access before the first password change.

## Verification checkpoint — 2026-11-16

- Angular production build: passed with 0 compilation errors.
- Angular build warning: initial bundle is 548.29 kB against the 500 kB budget; this is a performance follow-up, not a compile failure.
- Staff route, API model, API client method, and dashboard component are present.
- Staff-create request/response models, client method, and validated component form logic are present; the visible template action still needs to be completed and tested against the API.
- Staff-create form template is now present and Angular production compilation passes with 0 errors.
- EF migration `20261116213955_AddStaffMembership` was generated successfully using existing compiled outputs. It adds `AspNetUsers.MustChangePassword` and the `StaffMemberships` table.
- Final migration generation now succeeds after the clean Visual Studio rebuild. Database update reaches SQL Server but fails with connection error 5 / Named Pipes because the configured SQL Server instance is not accessible.
- Development database was recreated and all migrations applied successfully, including `AddStaffMembership`, `AddStaffAudit`, and `FinalStaffAuditModel`.
- Full Infrastructure/API build remains unverified because the local .NET SDK/MSBuild environment has previously stalled.
- The Visual Studio rebuild now reports 11 projects succeeded and 0 failed. A separate command-line solution build still exits without diagnostics, so both paths must be reconciled before claiming automated build verification.
- Domain build was attempted after the restore behavior change but the local build process stalled without output; no successful result is claimed.
- .NET automated test execution remains unverified because the local test host hangs after discovery.
- Focused Domain test execution was retried after the solution rebuild; the runner discovered the assembly but produced no test results and was stopped after hanging.
- Focused Application test execution was retried against the successful Visual Studio rebuild; the runner again discovered the assembly but produced no results and was stopped after hanging.

## Next implementation checkpoint

Implement staff lifecycle actions (disable, restore, role changes, password reset, and removal semantics) with owner-only API authorization, then add the corresponding management controls and tests.

## Current priority decision

- Front-end staff-management work is the immediate priority.
- Presentation and dashboard pages may use clearly labelled local demo data until database seeding is implemented.
- Borrower and lender dashboard presentation pages are now implemented with clearly labelled local demo data, responsive cards, and links into the existing profile flows.
- Login now routes borrowers to `/borrower` and lenders to `/lender`; existing profile routes remain available from the shared navigation.
- Angular production build passed with 0 compilation errors after the dashboard and routing changes. The existing initial-bundle budget warning remains a performance follow-up.
- Development startup now seeds one borrower and one lender through the existing registration commands, only when their configured email does not already exist. Demo credentials are development configuration only.
- The application therefore has dedicated authenticated entry pages for all three account types: borrower dashboard, lender dashboard, and staff management area.
- Lifecycle and regression tests are intentionally deferred, but remain required before release.
