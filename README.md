# ClearLend

![ClearLend phase 0 overview](project%20images/clearlend-phase-0-overview.png)

ClearLend is a fictional, educational lending marketplace and SaaS platform. It connects vetted borrowers with vetted lenders, while giving operations teams the controls, evidence, audit history, and operational visibility needed to run the marketplace responsibly.

This repository is a learning project. It uses synthetic identities, documents, decisions, and payments. It is not a production lending service, does not process real money, and does not provide legal, financial, or regulatory advice.

## Product vision

ClearLend aims to make the lending journey understandable and explainable:

> Everyone can see what they asked for, what was decided, and what happens next.

Borrowers should be able to register, complete vetting, find suitable products, apply, understand decisions, accept offers, and follow repayments. Lenders should be able to register, complete vetting, create products, review lending activity, and track repayments. Internal teams should be able to review evidence, support customers, reconcile payments, investigate exceptions, and demonstrate why important decisions were made.

![Business model and value exchange](project%20images/clearlend-business-model-value-exchange.png)

## Current status

The repository currently contains the runnable solution foundation and the first domain capability:

- .NET 10 solution using `ClearLend.slnx`.
- Modular-monolith project structure.
- Minimal ASP.NET Core API host.
- Liveness and readiness health endpoints.
- Domain-first `UserAccount` model.
- Strongly typed account identifiers and identity-provider subjects.
- Validated email value object.
- Typed domain results and error codes for expected business failures.
- Account lifecycle rules: pending, active, suspended, and closed.
- Focused domain tests.

The next business capability is participant onboarding: borrower and lender profiles, evidence collection, operational vetting, and accountable approval decisions.

## Solution structure

```text
ClearLend.slnx
│
├── src/
│   ├── ClearLend.Domain/                  # Business rules and domain model
│   ├── ClearLend.Application/             # Use cases and application ports
│   ├── ClearLend.Infrastructure/          # Persistence and external adapters
│   ├── ClearLend.Contracts/               # API request and response contracts
│   └── ClearLend.Api/                     # ASP.NET Core composition root
│
├── tests/
│   ├── ClearLend.Domain.Tests/
│   ├── ClearLend.Application.Tests/
│   ├── ClearLend.Architecture.Tests/
│   ├── ClearLend.Infrastructure.IntegrationTests/
│   └── ClearLend.Api.IntegrationTests/
│
├── project images/                        # Product and architecture diagrams
├── Directory.Build.props
├── Directory.Packages.props
└── global.json
```

### Dependency direction

```text
API ────────────────┬── Application ──── Domain
                    ├── Infrastructure ──┘
                    └── Contracts
```

The Domain project has no dependency on ASP.NET Core, EF Core, ASP.NET Core Identity, or any other ClearLend project. ASP.NET Core Identity will be integrated at the API/Infrastructure boundary and will provide the trusted identity subject used to locate a domain `UserAccount`.

## Business capabilities

ClearLend will be developed as a sequence of small, complete vertical slices rather than as a collection of disconnected tables or screens.

### Participant onboarding

The marketplace starts by establishing trust:

```text
UserAccount
    ↓
BorrowerProfile / LenderProfile
    ↓
VettingCase
    ↓
EvidenceItem
    ↓
VettingDecision
    ↓
Permission to apply or publish products
```

### Borrower operations

Borrowers will eventually manage profiles, applications, offers, loans, payments, notifications, and support conversations.

![Borrower operations](project%20images/clearlend-borrower-operations.png)

### Lender operations

Lenders will eventually manage profiles, lending limits, loan products, borrower applications, commitments, and repayment tracking.

![Lender operations](project%20images/clearlend-lender-operations.png)

### Review and operations

Operations teams will review evidence, make decisions, handle exceptions, and protect the boundaries between customer-facing information and internal notes.

![CRM and credit review operations](project%20images/clearlend-crm-review-operations.png)

![Business owner operations](project%20images/clearlend-business-owner-operations.png)

![Compliance operations](project%20images/clearlend-compliance-operations.png)

### Finance and servicing

Later capabilities will model settlement, reconciliation, payment allocation, fees, repayment schedules, missed payments, arrangements, and loan closure. Payment-provider requests will never be treated as proof that money moved until a reliable result is recorded.

![Finance and settlement operations](project%20images/clearlend-finance-settlement-operations.png)

![Servicing and collections operations](project%20images/clearlend-servicing-collections-operations.png)

![Fee model](project%20images/clearlend-fee-model.png)

![Loan economics example](project%20images/clearlend-loan-economics-example.png)

## Engineering principles

- Start with a modular monolith and introduce distribution only when measured evidence justifies it.
- Keep business rules in the Domain layer and organise application code by feature or vertical slice.
- Derive borrower ownership from a trusted authenticated actor; never accept ownership from request JSON or client-controlled headers.
- Keep public API contracts separate from domain entities and persistence rows.
- Represent expected business failures with typed results and stable machine-readable error codes.
- Use SQL Server optimistic concurrency for writes and opaque versions at the API boundary.
- Use UTC through `TimeProvider` and deterministic identifiers or clocks in tests where repeatability matters.
- Keep secrets outside source control and use synthetic data only.
- Avoid logging tokens, request bodies, financial evidence, document contents, or raw provider responses.
- Treat migrations, audit history, outbox records, and operational evidence as reviewed source code.
- Build accessibility, privacy, security, observability, and failure handling into each slice.

## Technology direction

| Area | Direction |
| --- | --- |
| Backend | .NET 10 and C# 14 |
| Web API | ASP.NET Core |
| Identity | ASP.NET Core Identity at the application boundary |
| Persistence | EF Core 10 and SQL Server |
| Frontend | Angular 21 |
| Architecture | Modular monolith with inward dependencies |
| Testing | Unit, architecture, integration, API, and browser journey tests |
| Operations | Health checks, structured logs, traces, metrics, audit, and outbox evidence |

## Getting started

### Prerequisites

- .NET SDK `10.0.400` or a compatible .NET 10 SDK.
- Visual Studio with .NET 10 support, or the .NET CLI.
- SQL Server will be required when persistence and integration tests are introduced.

The repository pins the expected SDK in [global.json](global.json).

### Restore, build, and test

From the repository root:

```powershell
dotnet restore ClearLend.slnx
dotnet build ClearLend.slnx --configuration Release
dotnet test ClearLend.slnx --configuration Release
```

### Run the API

```powershell
dotnet run --project src/ClearLend.Api/ClearLend.Api.csproj
```

The API currently exposes:

```text
GET /                 Basic service response
GET /health/live      Liveness check
GET /health/ready     Readiness check
```

The API is intentionally small at this stage. Business endpoints will be added only after their domain rules, ownership behavior, contracts, and tests are defined.

## Delivery roadmap

1. Establish the solution and architecture boundaries. **Complete**
2. Model participant registration and onboarding. **Next**
3. Add borrower and lender profiles.
4. Add evidence collection and vetting decisions.
5. Add lender product publication.
6. Add borrower draft applications and submission.
7. Add offers, commitments, and synthetic settlement.
8. Add loan servicing, schedules, payments, reconciliation, and fees.
9. Add support, compliance, notifications, reporting, and audit workflows.
10. Add the Angular journeys, operational evidence, performance workload, and delivery automation.

Roadmap status will only be marked complete when the relevant implementation, tests, documentation, and runtime evidence exist.

## Product reference diagrams

The repository includes the provided diagrams used to explain the product and architecture:

![Requirement to evidence](project%20images/clearlend-requirement-to-evidence.png)

![Phase 0 overview](project%20images/clearlend-phase-0-overview.png)

The full collection is stored in [`project images`](project%20images/).

## Project guidance

The detailed implementation and mentoring requirements are recorded in [CLEARLEND-JUNIOR-DEVELOPER-AI-BUILD-PROMPT.md](CLEARLEND-JUNIOR-DEVELOPER-AI-BUILD-PROMPT.md). That document defines the engineering rhythm, decision checkpoints, acceptance evidence, security rules, testing expectations, and later implementation phases.

## Disclaimer

ClearLend is fictional software for education and engineering practice. It is not open for real borrowing, lending, investment, or payment processing. Any future implementation remains synthetic unless explicitly replaced with separately reviewed, legally compliant, production-grade integrations.
