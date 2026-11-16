# ClearLend Development Working Agreement

Work with Faz Ahmed as a senior developer and architect partner. Use small, verifiable steps and do not rush large feature changes.

The quality bar is production-team discipline. Faz must not be the person who discovers compile errors, mismatched validators, broken event bindings, stale documentation, or untested assumptions.

## Before coding

- Inspect the existing architecture, conventions, namespaces, tests, styling, and documentation.
- Explain the proposed change in plain English.
- List the files to create or modify.
- Identify assumptions, risks, and open questions.

## Implementation sequence

Implement one logical layer at a time:

1. Domain
2. Application
3. Infrastructure
4. API
5. Frontend
6. Tests
7. Documentation

After each layer, compile or validate that layer before continuing. Follow existing naming, analyzer, formatting, architecture, and UI conventions.

Never make a large cross-layer change in one unverified edit. Prefer small edits followed by a focused check. Do not proceed to the next layer while the current layer has errors.

## Definition of done

Do not describe a feature as complete until:

- The affected code builds.
- Tests are added or updated.
- Required database migrations exist.
- Frontend routes and API contracts match.
- The end-to-end happy path is tested, or explicitly marked unverified.

Always distinguish between:

- Implemented but not compiled.
- Compiled but not tested.
- Compiled and tested.
- End-to-end verified.

If a build or test cannot be run, say so clearly. Never imply that it passed.

Before reporting completion, perform a consistency audit:

- Compare every frontend validator with the backend validator and Identity policy.
- Check every template event binding calls the intended method, for example `(ngSubmit)="submit()"`.
- Check every route import points to a real file.
- Check every API model matches the request and response contract.
- Check every required database entity has mapping, repository registration, and migration.
- Check every new namespace and shared type import.
- Check analyzer naming rules such as `cancellationToken`.
- Check that user-facing labels describe the actual validators and rules.
- Check that styles are scoped to the component that owns the markup.

## Scope and safety

- Resolve compiler and analyzer errors before adding more code.
- Keep changes focused and avoid unrelated modifications.
- Do not create raw or placeholder UI when an established design system exists.
- Do not commit or push unless Faz explicitly asks.
- Do not start or stop applications unless Faz explicitly asks.

## Angular UI standards

Before changing a page, inspect the existing visual language. Reuse the established Angular Material components, typography, spacing, colors, responsive layout, and component-scoped styles. New pages should have clear component, template, and stylesheet files unless there is a documented reason otherwise.

## Backend standards

Before adding a domain type, inspect related value objects, enums, records, and namespaces. Before adding handlers or repositories, inspect interface parameter names and analyzer rules. Build the affected backend project immediately after each backend layer.

## Final report

Every completed step must report:

- What changed.
- What was verified.
- What was not verified.
- Remaining issues.
- Exact manual test steps.

Do not ask Faz to repeatedly clean and rebuild while avoidable errors remain. Run the relevant validation yourself first. If the environment prevents validation, identify the exact command and limitation, and mark the work unverified.
