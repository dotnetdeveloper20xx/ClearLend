# ClearLend Developer Diary — Day 1

**Date:** 8 October 2026

## What ClearLend is

ClearLend is a fictional educational lending marketplace. Borrowers are customers who want to borrow money, while lenders provide the money and create lending products.

The project uses synthetic people, documents, decisions, and payments. It is not a real lending service and does not process real money.

The main business idea is:

> Everyone should be able to see what they asked for, what was decided, and what happens next.

## What we created first

We started with an empty repository containing the project prompt and basic repository files. Before building business functionality, we created a clean Visual Studio/.NET solution foundation.

## Solution and projects

The solution is named `ClearLend.slnx` and targets .NET 10 with SDK version `10.0.400`.

### Production projects

| Project | Purpose |
| --- | --- |
| `ClearLend.Domain` | Contains the most important business rules, entities, value objects, states, and typed domain results. It does not depend on ASP.NET Core, EF Core, or other ClearLend projects. |
| `ClearLend.Application` | Will contain use cases, commands, queries, orchestration, and application interfaces. It depends on the Domain. |
| `ClearLend.Infrastructure` | Will contain EF Core, SQL Server, persistence, ASP.NET Identity integration, and external service adapters. It depends inward on Application and Domain. |
| `ClearLend.Contracts` | Will contain public API request and response models. It is kept separate from domain entities and database models. |
| `ClearLend.Api` | The ASP.NET Core host and composition root. It will eventually expose authentication, endpoints, middleware, health checks, OpenAPI, and dependency injection. |

### Test projects

| Project | Purpose |
| --- | --- |
| `ClearLend.Domain.Tests` | Tests business rules and entity state transitions. |
| `ClearLend.Application.Tests` | Tests application use cases and orchestration. A dependency-boundary test was added today. |
| `ClearLend.Architecture.Tests` | Tests dependency direction between layers. |
| `ClearLend.Infrastructure.IntegrationTests` | Will test persistence and SQL Server behavior. A dependency-boundary test was added today. |
| `ClearLend.Api.IntegrationTests` | Will test real HTTP behavior. An API assembly smoke test was added today; endpoint tests remain future work. |

## Dependency direction

The intended dependency direction is inward:

```text
API ────────────────┬── Application ──── Domain
                    ├── Infrastructure ──┘
                    └── Contracts
```

The Domain must remain independent. The API and Infrastructure can depend on inner layers, but the Domain must never depend on them.

## API foundation

The API currently has a small ASP.NET Core host with:

```text
GET /                 Basic service response
GET /health/live      Liveness check
GET /health/ready     Readiness check
```

The API is intentionally small because we agreed to define the business model before adding application endpoints.

## Business modelling decision

We decided that ClearLend is like a new shop opening before it has anything to sell:

- Borrowers are customers.
- Lenders are suppliers of lending capital.
- Neither side should be allowed to use marketplace features immediately.
- Both sides must register, complete a profile, provide evidence, and pass operational vetting.
- Only approved borrowers should later be allowed to apply for loans.
- Only approved lenders should later be allowed to create or publish products.

The initial business flow is:

```text
User account
    ↓
Borrower or lender profile
    ↓
Vetting case
    ↓
Evidence
    ↓
Vetting decision
    ↓
Permission to use marketplace capabilities
```

## Domain entities and classes created

### Identity classes

| Class | Reason |
| --- | --- |
| `UserAccount` | Represents the authenticated person and account lifecycle. It is separate from borrower and lender business profiles. |
| `UserAccountId` | Strongly typed account identifier so account IDs cannot easily be confused with profile or case IDs. |
| `UserAccountRegistration` | Groups the information needed to register an account instead of passing many parameters. |
| `IdentityProviderSubject` | Stores the stable subject identifier supplied by the trusted identity provider. It is not based on email. |
| `EmailAddress` | Validates and normalises email addresses as a value object. |
| `AccountStatus` | Defines `Pending`, `Active`, `Suspended`, and `Closed` account states. |

`UserAccount` supports registration, email changes, activation, suspension, and closure. Expected failures return typed domain results instead of random exceptions.

ASP.NET Core Identity has not been placed in the Domain. It will later be integrated at the API/Infrastructure boundary. The Domain will receive the trusted identity-provider subject rather than depending on `IdentityUser`.

### Borrower classes

| Class | Reason |
| --- | --- |
| `BorrowerProfile` | Represents the borrower’s business role and profile state. It is separate from the authentication account. |
| `BorrowerProfileId` | Strongly typed borrower-profile identifier. |
| `BorrowerProfileCreation` | Groups the information needed to create a borrower profile. |
| `PersonalName` | Validated value object containing given name and family name. |
| `ProfileState` | Defines `Incomplete`, `ReadyForReview`, `Active`, `Suspended`, and `Closed`. |

`BorrowerProfile` can be completed and submitted for review. Activation requires an approved borrower decision for the matching account.

### Vetting classes

| Class | Reason |
| --- | --- |
| `VettingCase` | Represents the operational review of a borrower or lender. It owns the review lifecycle. |
| `VettingCaseId` | Strongly typed vetting-case identifier. |
| `VettingCaseOpening` | Groups the participant account, subject type, and opening timestamp. |
| `VettingCaseStatus` | Defines `Open`, `InReview`, `AwaitingInformation`, `Approved`, `Rejected`, `Suspended`, and `Closed`. |
| `VettingSubjectType` | Identifies whether a case belongs to a `Borrower` or `Lender`. |
| `ReviewerAssignment` | Groups reviewer account and assignment timestamp. |
| `VettingDecision` | Immutable record of the decision made about a vetting case. |
| `VettingDecisionId` | Strongly typed decision identifier. |
| `VettingDecisionDetails` | Groups the case, subject, outcome, reason, reviewer, policy version, and decision timestamp. |
| `DecisionOutcome` | Defines `Approved`, `Rejected`, `MoreInformationRequired`, and `Restricted`. |
| `DecisionReason` | Validated explanation for a decision. |
| `PolicyVersion` | Records which policy version was used for the decision. |

Vetting cases cannot be approved or rejected directly. They must apply a matching `VettingDecision`, which records the reason, reviewer, subject, and policy version.

Suspended cases can explicitly resume review.

### Evidence classes

| Class | Reason |
| --- | --- |
| `EvidenceItem` | Represents evidence requested or submitted for a vetting case. It stores metadata and review state, not document contents. |
| `EvidenceItemId` | Strongly typed evidence identifier. |
| `EvidenceRequest` | Groups the case, evidence type, and request timestamp. |
| `EvidenceSubmission` | Groups the protected storage reference and submission timestamp. |
| `EvidenceRejection` | Groups the rejection reason and review timestamp. |
| `StorageReference` | Represents an opaque protected-storage reference rather than a public file URL. |
| `EvidenceType` | Defines identity, address, organisation, ownership, financial capacity, and other evidence categories. |
| `EvidenceStatus` | Defines `Requested`, `Submitted`, `Accepted`, `Rejected`, `Expired`, and `Superseded`. |

Evidence replacement creates a new item instead of overwriting historical evidence. Evidence contents remain outside the Domain and will later be handled by Infrastructure.

### Shared domain support

| Class | Reason |
| --- | --- |
| `DomainError` | Contains a stable error code and safe message. |
| `DomainResult` | Represents success or failure for operations without a value. |
| `DomainResult<T>` | Represents success or failure for operations that return a value. |
| `DomainResults` | Provides factory methods for typed successes and failures. |
| `UtcTimestamp` | Validates UTC timestamps at the value-object boundary. |

The project uses typed business failures such as `identity.email.invalid` and `vetting.decision.case_mismatch`. These can later be mapped by the Application/API layers to appropriate HTTP responses.

## Testing completed

The Domain test suite covers:

- User-account lifecycle transitions.
- Borrower-profile completion and activation rules.
- Vetting-case transitions.
- Subject-specific vetting decisions.
- Evidence submission, acceptance, rejection, expiry, and replacement behavior.
- Typed invalid-ID results.
- UTC timestamp validation.
- Layer dependency direction.
- API assembly presence.

The latest focused Domain test run passed with 29 tests. The full solution also has executable tests in the Application, Infrastructure, Architecture, and API test projects.

## Review improvements made during Day 1

We performed strict code reviews and corrected several issues:

- Removed direct vetting approval and rejection methods.
- Required decisions to match the correct vetting case and participant.
- Required borrower activation to use an approved borrower decision.
- Replaced long parameter lists with named request/details objects.
- Replaced expected business exceptions with typed results.
- Hardened typed identifiers with private constructors and typed factories.
- Added safe result accessors for tests and internal code.
- Replaced exception-based email parsing with a source-generated regular expression.
- Added explicit suspended-case recovery.
- Removed template `Class1` files.
- Added architecture and boundary tests.

## Documentation and diagrams

The README was expanded with the product vision, architecture, roadmap, setup instructions, and the provided diagrams in the `project images` folder.

The repository also contains [CLEARLEND-JUNIOR-DEVELOPER-AI-BUILD-PROMPT.md](../CLEARLEND-JUNIOR-DEVELOPER-AI-BUILD-PROMPT.md), which defines the implementation rules, decision process, security expectations, and definition of done.

## Git history and remote work

The following work was committed and pushed to the ClearLend remote during Day 1:

| Commit | Description |
| --- | --- |
| `c8f1148` | Established the solution and initial UserAccount domain model. |
| `ce69812` | Added the comprehensive README and 13 project diagrams. |
| `8ade497` | Strengthened domain onboarding invariants. |
| `cd57125` | Hardened domain APIs and test boundaries. |

## What remains for the next day

- Finish independently validating `VettingDecisionDetails`.
- Add deterministic ID generation where repeatable tests require it.
- Add real HTTP API behavior tests for the health endpoints and future application endpoints.
- Begin the Application layer with participant onboarding use cases.
- Add ASP.NET Core Identity integration at the correct boundary.
- Add EF Core and SQL Server persistence only after the Domain and Application contracts are stable.
- Add real SQL Server integration tests for ownership, migrations, concurrency, and transactions.

## Plain-English summary

Today we built the empty ClearLend solution into a structured .NET foundation. We agreed that the product must first know who its borrowers and lenders are, review their evidence, and record accountable decisions before either side can use lending features. We created the first identity, borrower, vetting, evidence, and decision domain classes, added typed and safer results, wrote domain and architecture tests, documented the product with diagrams, reviewed the code strictly, and pushed the work to GitHub.
