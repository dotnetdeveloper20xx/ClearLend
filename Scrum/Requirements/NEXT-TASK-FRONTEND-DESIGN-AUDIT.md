# Next Task — ClearLend Frontend Design Audit and Upgrade

Status: In progress — major visual redesign slice implemented

## Objective

Inspect the existing ClearLend frontend and create an evidence-based plan to make it feel like a credible, modern financial-services platform. The work must improve typography, colour, navigation, tabs, buttons, links, headings, forms, empty states, responsiveness, and accessibility without inventing unsupported business functionality.

## Working prompt

Act as a senior product designer, UI/UX expert, accessibility specialist, and front-end architect. Inspect the existing ClearLend application, its routes, components, styles, screenshots, `AGENTS.md`, and requirements documents before proposing or implementing changes. Treat the codebase as the source of truth. Classify every feature as implemented, mocked, planned, unverified, or blocked. Do not invent business workflows, silently fake API behaviour, or present planned features as complete.

Audit the application shell, navigation, typography, colour system, spacing, buttons, links, tabs, cards, tables, forms, modals, alerts, pagination, empty/loading/error/success states, responsive behaviour, accessibility, Angular Material usage, route destinations, API calls, and mock data. Identify dead controls, misleading labels, inconsistent patterns, and repeated styling.

Produce an evidence-based design system and page-by-page implementation plan covering existing public, borrower, lender, staff, profile, dashboard, registration, login, and password pages. Specify target fonts, colours, spacing, breakpoints, component states, keyboard behaviour, content rules, shared components, risks, assumptions, files to change, acceptance criteria, build/test commands, and manual verification steps.

Implement in small verified phases only after the audit. Preserve existing behaviour and contracts. Check every route, link, event binding, form, validator, API model, and responsive layout. Build and test each affected layer. Do not report completion without evidence.

## Required output

- Current-state audit.
- Target design system.
- Page-by-page redesign plan.
- Shared component plan.
- Route and interaction audit.
- Implementation roadmap.
- Risks, assumptions, and decisions.
- Verified, unverified, mocked, planned, and blocked items.
- Exact manual test steps.

## Initial evidence-based audit

### Strengths

- The application has a coherent ClearLend navy, teal, and gold palette, a reusable shell, Material controls, visible focus styling, and responsive breakpoints.
- The owner-demo drawer exposes the current implemented routes and labels future areas as planned.
- Borrower profile, lender profile, and staff management already use tabs to reduce scrolling and group related work.
- Registration and login forms use Angular reactive forms with explicit submit handlers.

### Issues to address

- The global Material theme still uses the Azure palette while the ClearLend brand tokens use navy, teal, and gold; this can produce inconsistent Material controls.
- The shell and welcome page contain dense one-line markup and page-specific styles, making consistent refinement difficult.
- Profile edit buttons are currently visual demo controls without an action or explicit planned/demo treatment.
- Dashboard activity links use in-page anchors for mocked sections; these should be documented as demo navigation or replaced with clear disabled/planned states when a destination is not implemented.
- The current demo menu is useful for review but needs stronger active-route styling, clearer mobile behaviour, and a more deliberate distinction between navigation and planned previews.
- Shared button, link, tab, card, form, status, and empty-state patterns are not yet formalized as reusable components or documented tokens.
- Existing build warnings show the initial bundle and several component styles exceed configured budgets.
- Authentication guards were relaxed for browsing and must remain explicitly marked as demo-only until restored.

### First implementation slice

1. Formalize design tokens and Material theme alignment.
2. Normalize shared button, link, heading, focus, and tab states.
3. Improve shell navigation active states and mobile drawer behaviour.
4. Classify or replace visual-only controls and mock anchors.
5. Rebuild and run the static interaction audit before moving to page-specific refinements.

### Current verification

- Angular build previously passed with zero compilation errors.
- Route declarations, shell links, profile tabs, staff tabs, forms, and event bindings have been inspected.
- Runtime visual review and browser automation remain unverified until the application is started and the browser test provider is available.

## Implementation checkpoint — shared layer

- Added active-route styling to the primary navigation and owner-demo navigation.
- Aligned Material button colour variables with the ClearLend teal brand tokens while retaining the supported Angular Material base palette.
- Marked profile edit actions as explicitly planned and disabled until an edit workflow exists, removing misleading dead controls.
- Rebuilt Angular successfully with zero compilation errors.
- Remaining build warnings are bundle/style budget warnings, not compilation failures.
- `git diff --check` reported no whitespace errors; the test command remains blocked by the missing Angular/Vitest browser provider package.

## Major visual redesign checkpoint

- Reworked the application shell with a stronger navy-to-teal banking-style header treatment and a soft branded page background.
- Rebuilt the welcome page hero into a high-contrast corporate presentation with a clear announcement strip, journey panel, responsive hero layout, and three distinct role pathways.
- Added responsive behaviour for the hero and pathway cards so the page remains usable on smaller screens.
- Angular production build passed with zero compilation errors after the redesign.

## Definition of done

- Angular builds with zero compilation errors.
- Existing tests pass or failures are explicitly explained.
- No visible dead links or misleading controls remain.
- Forms, tabs, buttons, routes, and responsive states are checked.
- Mock data is clearly labelled.
- Accessibility and visual consistency are reviewed.
- Documentation matches the actual implementation.
