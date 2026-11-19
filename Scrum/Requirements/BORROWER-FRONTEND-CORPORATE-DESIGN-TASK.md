# ClearLend Borrower Frontend — Corporate Editorial Design Task

## Business objective

Transform the borrower-facing ClearLend frontend from a basic form-and-dashboard experience into a polished, trustworthy financial-services website and workspace.

The supplied CyberArk screenshot is a visual reference only. It establishes the desired design direction:

- A clean corporate header with clear primary navigation.
- A strong secondary navigation band for relevant sections.
- Clear, prominent page headings.
- A structured editorial card grid rather than a plain list of links.
- A mixture of large featured content and smaller supporting cards.
- Strong visual hierarchy, restrained colours, consistent spacing, and clear calls to action.

Do not copy CyberArk branding, logos, wording, imagery, or proprietary layout. Use ClearLend’s own identity, content, routes, and lending-domain terminology.

## Scope

This task focuses on the borrower experience first:

- Welcome and borrower entry experience.
- Borrower registration.
- Borrower dashboard.
- Borrower profile.
- Borrower funding journey previews.
- Shared shell, navigation, cards, forms, tabs, buttons, and responsive behaviour used by those pages.

Lender and staff screens should not be redesigned as part of this task unless a shared component change requires a compatible update. Any shared change must be regression-tested against those roles.

## Confirmed versus mocked scope

### Confirmed implementation

- Existing ClearLend routes and borrower pages.
- Existing registration, login, profile, dashboard, and password flows.
- Existing backend contracts unless a separate approved requirement changes them.

### Demo/mock content allowed

- Featured borrower guidance cards.
- Example funding journey stages.
- Example application summary cards.
- Educational or explanatory content clearly marked as demo content where it is not yet backed by the API.

### Not to be invented

- New lending decisions or credit rules.
- Financial promises, rates, approvals, or eligibility claims.
- Unapproved application workflow behaviour.
- Fake API success presented as a completed production capability.
- Real customer, lender, or financial data.

## Target experience

### 1. Corporate application shell

Create a consistent borrower shell with:

- ClearLend logo/wordmark area.
- Primary navigation for Home, How it works, Borrower journey, and Support.
- A prominent sign-in or account action.
- A secondary navy navigation bar where appropriate for borrower sections.
- Active route styling.
- Responsive mobile navigation with keyboard support.
- Clear page container widths and consistent spacing.
- Breadcrumbs or contextual navigation on deeper pages where useful.

The existing owner-demo menu must remain available for review, but it must be visually separated from the customer-facing navigation and clearly labelled as demo navigation.

### 2. Borrower welcome experience

Create a corporate landing layout containing:

- A strong page heading explaining the ClearLend value proposition.
- A primary borrower call to action.
- A secondary “How it works” or guidance action.
- A featured borrower journey panel.
- A grid of supporting cards for:
  - Building a borrower profile.
  - Preparing a funding request.
  - Understanding the review process.
  - Keeping information up to date.
- Clear distinction between available actions and planned previews.

### 3. Borrower registration

Redesign registration as a professional onboarding form:

- Introductory explanation of why the information is needed.
- Group fields into logical sections.
- Clear required/optional indicators.
- Consistent labels and helper text.
- Inline validation and useful error messages.
- Password guidance matching the actual validator.
- Consent section with accessible checkbox treatment.
- Submission loading state.
- Success state with next-step guidance.
- API error state with a recovery instruction.
- Existing reactive form and backend contract must remain correct.

### 4. Borrower dashboard

Present the dashboard as an operational journey rather than a collection of unrelated cards:

- Welcome and account status area.
- Profile completion/readiness indicator.
- “Your next step” panel.
- Funding request summary.
- Application status timeline or status cards.
- Supporting guidance/content cards.
- Empty state when no application exists.
- Clear actions back to the profile or registration journey.

Every action must either navigate to a real route, perform a verified action, or be clearly labelled as planned/demo content.

### 5. Borrower profile

Use a structured profile workspace with:

- Overview tab.
- Personal/account details tab.
- Funding readiness or next steps tab.
- Clear completion indicators.
- Read-only presentation where editing is not implemented.
- Explicit planned labels for unavailable edit functionality.
- Consistent status badges and explanatory copy.
- Responsive tab behaviour on small screens.

### 6. Borrower funding journey preview

If the full funding-request API is not ready, provide a clearly labelled demo preview rather than pretending it is live. The preview should show:

- Draft request.
- Submitted request.
- Under review.
- More information required.
- Decision stage.

Each stage must explain the business meaning and the borrower’s expected next action. Do not show fabricated approval decisions or financial terms.

## Design system requirements

Define or reuse shared tokens for:

- Navy, teal, gold, neutral, success, warning, error, and information colours.
- Font family, heading hierarchy, body text, labels, captions, and line heights.
- 4px/8px-based spacing scale.
- Container widths and responsive breakpoints.
- Card radius, border, shadow, and hover behaviour.
- Primary, secondary, tertiary, destructive, disabled, and loading buttons.
- Internal, external, unavailable, and planned links.
- Tabs with active, hover, focus, disabled, and overflow states.
- Form fields, validation, helper text, and error states.
- Empty, loading, error, success, and demo states.

The design should feel corporate and distinctive without becoming visually heavy, decorative, or unrelated to financial services.

## Angular implementation tasks

1. Inspect existing ClearLend shell, routes, components, styles, Angular Material configuration, and page contracts.
2. Record the current baseline build and test status.
3. Create or refine shared design tokens in the global stylesheet.
4. Refine the application shell and responsive navigation.
5. Create reusable presentation components where repetition exists:
   - Page header
   - Section heading
   - Content card
   - Featured card
   - Status badge
   - Progress/readiness indicator
   - Journey step/timeline
   - Empty state
   - Demo notice
6. Redesign the borrower welcome page.
7. Redesign the borrower registration page without changing its backend contract.
8. Redesign the borrower dashboard using real or clearly labelled mock data.
9. Redesign the borrower profile tabs.
10. Add the borrower funding journey preview only as a documented demo feature if the API is not available.
11. Check all routes, links, buttons, tabs, forms, and event bindings.
12. Check mobile, tablet, desktop, and wide desktop layouts.
13. Update page and feature documentation.

## Backend and contract safety

This is primarily a frontend design task. Do not change domain models, database schema, migrations, API routes, DTOs, or validation rules unless a frontend contract mismatch is found.

If a mismatch is found:

1. Document the mismatch.
2. Identify whether the correct fix belongs in frontend or backend.
3. Preserve compatibility with existing flows.
4. Add or update tests before changing the contract.
5. Report the change separately from the visual work.

## Accessibility requirements

- Use semantic headings in order.
- Every form control must have an accessible label.
- Every button must state its action or be visibly disabled when unavailable.
- Every link must have a valid destination or an explicit planned/unavailable treatment.
- Tabs must be keyboard accessible and retain visible focus.
- Colour must not be the only way to communicate status.
- Text and controls must meet accessible contrast expectations.
- Layout must remain usable at mobile widths and increased text size.
- Error and success messages must use appropriate live-region semantics.

## Verification requirements

Before calling the task complete:

- Build Angular with zero compilation errors.
- Run all available frontend tests.
- Confirm every affected route loads.
- Confirm every registration and login form submits through its intended handler.
- Confirm validators and user-facing guidance match.
- Confirm every tab, button, link, and menu action behaves correctly.
- Confirm no empty or misleading links remain.
- Confirm mock content is clearly identified.
- Confirm lender, staff, and existing public pages have not regressed.
- Perform a responsive review at mobile, tablet, desktop, and wide desktop sizes.
- Record any test or environment limitation explicitly.

## Delivery report to Faz

The final report must use business language and explain:

- What was improved for borrowers.
- How the new design improves clarity, trust, and conversion.
- Which screens and components were changed.
- Which workflows are real and which remain demo/mock.
- What was compiled and tested.
- What was manually reviewed.
- What remains unverified.
- Any risks or decisions required.
- Exact manual steps for reviewing the result.

Do not describe the borrower experience as production-ready until the runtime journey, API-backed submission, validation, responsive behaviour, and regression checks have been verified.

## Detailed execution plan and quality gates

### Phase 0 — Baseline and evidence

Before editing:

- Read `AGENTS.md` and this requirement document.
- Inspect the current route table and identify every borrower route.
- Inspect the borrower components, templates, styles, services, models, guards, and tests.
- Inspect the backend request/response contracts used by registration, login, profile, and dashboard screens.
- Record the current Angular build result.
- Record the current frontend test result.
- Record existing warnings and known environment limitations.
- Capture the current page list and link destinations.

Gate: no implementation starts until the baseline and affected files are recorded.

### Phase 1 — Information architecture and screen map

Produce a screen map for:

- Public welcome page.
- How-it-works page.
- Borrower registration.
- Login.
- Borrower dashboard.
- Borrower profile.
- Change password.
- Funding journey preview or future funding-request page.

For each screen record:

- User objective.
- Primary action.
- Secondary action.
- Data source.
- Permission requirement.
- Real, mocked, planned, or unavailable status.
- Loading, empty, error, success, and disabled states.

Gate: every page has a defined purpose and no screen is added only because it looks attractive.

### Phase 2 — Shared design foundation

Inspect and update only the shared layer first:

- Global colour tokens.
- Typography and heading hierarchy.
- Spacing and container rules.
- Button hierarchy and states.
- Link and planned-action treatment.
- Card and surface patterns.
- Focus and keyboard-visible states.
- Responsive breakpoints.
- Material theme overrides.

Files to inspect or modify may include:

- `src/ClearLend.Web/src/styles.scss`
- `src/ClearLend.Web/src/app/app.ts`
- `src/ClearLend.Web/src/app/app.html`
- `src/ClearLend.Web/src/app/app.scss`

Gate: Angular builds before page-specific changes begin.

### Phase 3 — Shell and navigation

Verify and implement:

- ClearLend brand area.
- Primary navigation.
- Owner-demo navigation separation.
- Active route state.
- Mobile navigation behaviour.
- Keyboard navigation.
- Sign-in/sign-out states.
- Account role visibility.
- Valid route destinations.
- No dead or unexplained links.

Gate: every visible navigation control is mapped to a real route or explicitly labelled planned.

### Phase 4 — Borrower welcome page

Implement and verify:

- Corporate header and page introduction.
- Clear borrower call to action.
- Secondary guidance call to action.
- Featured borrower journey panel.
- Supporting content card grid.
- Demo labels where content is illustrative.
- Responsive card layout.
- Hover and focus states.
- Accessible heading order.

Gate: a new visitor can understand ClearLend’s purpose and start the borrower journey without explanation.

### Phase 5 — Borrower registration

Inspect the complete form chain:

- Angular form model.
- Template bindings.
- Client validators.
- Backend DTO.
- Backend validators.
- Identity password policy.
- API endpoint and response handling.
- Duplicate-account handling.
- Consent handling.
- Loading state.
- Success routing.
- Error display.

Then implement the visual redesign without changing behaviour accidentally.

Gate: invalid input is rejected clearly, valid input reaches the intended handler, and success/error states are understandable.

### Phase 6 — Borrower dashboard and profile

Implement and verify:

- Account summary.
- Profile readiness.
- Next-step guidance.
- Application/request summary.
- Status presentation.
- Empty state when no application exists.
- Profile overview tab.
- Account details tab.
- Funding readiness tab.
- Planned edit actions clearly disabled or routed only when implemented.

Gate: the borrower can understand their current position and next action without relying on internal terminology.

### Phase 7 — Funding journey preview

If the API is not ready:

- Use clearly labelled mock data.
- Do not fabricate approval, decline, rates, or financial terms.
- Explain the meaning of each status.
- Show the expected borrower next action.
- Keep the preview visually compatible with the future live workflow.

If the API is ready:

- Verify DTOs, routes, loading, error, retry, and success handling.
- Verify persistence and refresh behaviour.
- Verify permissions.

Gate: the demo does not misrepresent planned functionality as live business capability.

### Phase 8 — Regression and accessibility review

Check the unaffected areas:

- Lender registration.
- Lender dashboard.
- Lender profile.
- Staff management.
- Login.
- Change password.
- How it works.
- Owner-demo navigation.

Check accessibility:

- Keyboard-only navigation.
- Visible focus.
- Screen-reader labels.
- Heading order.
- Form labels and errors.
- Tab keyboard behaviour.
- Contrast.
- Zoom and text resizing.
- Mobile viewport.

Gate: no borrower redesign change breaks another role or shared shell.

### Phase 9 — Verification and sign-off

Run:

- `npm run build` in `src/ClearLend.Web`.
- Available frontend tests.
- Backend build if a contract is changed.
- Backend tests if a contract is changed.
- `git diff --check`.
- Route and event-binding audit using `rg`.
- Manual browser review of every affected route.

Record each result as one of:

- Implemented and compiled.
- Compiled and tested.
- Manually verified.
- Mocked/demo only.
- Planned.
- Unverified.
- Blocked.

The task cannot be signed off while a known compile error, broken route, dead action, mismatched validator, or misleading mock feature remains.

### Required final artefacts

- Updated borrower frontend code.
- Updated shared design styles/components.
- Updated tests where behaviour changed.
- Updated documentation.
- Page and route verification record.
- Business-language summary for Faz.
- Exact manual test instructions.
- List of remaining risks and future decisions.

## Progress checkpoint — borrower registration

- Reworked the registration page into a two-column corporate onboarding layout.
- Added a clear borrower journey explanation beside the form.
- Grouped the form into “About you” and “Secure access” sections.
- Preserved the existing reactive form, validators, API call, consent requirement, loading state, success state, and error state.
- Improved responsive behaviour for tablet and mobile layouts.
- Angular production build passed with zero compilation errors after the change.
- Existing bundle and stylesheet budget warnings remain and are recorded as non-blocking technical debt.

## Progress checkpoint — borrower dashboard

- Reworked the borrower dashboard into a guided workspace rather than a group of generic summary cards.
- Added a clear account header and borrower identity treatment.
- Added a prominent “Your next step” panel with a real profile route.
- Added profile readiness, funding request count, and account status summary cards.
- Added application/request cards with status, progress indicator, amount, and clearly labelled demo guidance.
- Added a guidance panel linking to the existing How ClearLend Works page.
- Added responsive layouts for smaller screens.
- Preserved the existing local demo application data and routes.
- Angular production build passed with zero compilation errors after the change.

## Progress checkpoint — borrower profile

- Added a clearer borrower identity summary above the profile workspace.
- Improved the profile page hierarchy so account identity and account status are visible before the tabs.
- Retained the existing Overview, Account details, and Next steps tabs.
- Added responsive handling for the identity summary and tab content.
- Kept edit functionality explicitly planned because no edit API/workflow is currently available.
- Angular production build passed with zero compilation errors after the change.

## Progress checkpoint — lender consistency update

- Reworked lender registration to use the same corporate onboarding pattern as borrower registration.
- Added business-detail and account-contact sections, lender journey guidance, consistent field treatment, and responsive layout.
- Preserved the existing lender form model, validation, consent requirement, API submission, and error message handling.
- Reworked the lender dashboard to use the same structured workspace pattern as the borrower dashboard.
- Added lender identity, profile readiness action, opportunity summary, opportunity preview cards, and explicit demo/planned wording.
- Added the missing `MatCardModule` import required by the redesigned lender registration template.
- Angular production build passed with zero compilation errors after the lender changes.
