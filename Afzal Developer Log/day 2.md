# ClearLend Development Log — Day 2

**Date:** 9 October 2026  
**Status:** In progress
**Focus:** Prepare the internal office that will receive, route, review, and account for work created when the marketplace begins accepting customers.

## The day’s story, told for a business owner

Yesterday, ClearLend gained the beginnings of a customer journey and the business rules behind accounts, borrower profiles, evidence, and vetting. Today we looked at the other side of that journey: the office that must handle the work after it arrives.

Picture a new lending office before opening day. A customer’s request arrives. Someone must own it, the right team must receive it, and staff must know what they are allowed to do. If the person handling the work is away, the task must return to the team or pass to another team without losing its history. If a concern arises, it must be raised, explained, and eventually resolved. A decision must be tied to the correct case and reviewer. Managers need to know what happened and who took each important action.

That is the business problem Day 2 set out to solve. We built and strengthened the rules and application workflows for staff, roles, permissions, team queues, operational work items, assignment, handover, escalation, and audit records. We also reviewed the existing vetting and evidence rules because those are the business records office staff will eventually use during a review.

The intended operating story is:

```text
A trusted first owner is established
        ↓
Staff are invited, activated, and given responsibilities
        ↓
Queues are created for the teams that handle different kinds of work
        ↓
Eligible staff join the queues
        ↓
A synthetic intake task enters the Operations queue
        ↓
An active staff member takes responsibility
        ↓
Work can be returned, reassigned, or transferred with its history intact
        ↓
Concerns can be escalated and later resolved
        ↓
A case decision can be tied to its subject and assigned reviewer
        ↓
The office can retain an audit trail and, once queries exist, retrieve it
```

The code now contains several of these rules and command building blocks. It does not yet implement or prove the whole journey. In particular, the Application workflows for reviewing evidence and cases, the office’s read queries, and the end-to-end rehearsal are still outstanding. Day 2 therefore leaves us with a stronger shop-floor foundation, not an office that is ready to open.

## What the code is doing in business terms

The Domain layer describes the office’s rules: who a staff member is, what their roles mean, how work changes state, and what must be true before a decision is accepted. These rules are kept independent of databases and screens so they remain the business’s source of truth.

The Application layer describes office actions: invite a staff member, create a queue, assign or transfer work, or record an audit entry. A command represents a requested action; a handler coordinates the required business objects and repository contracts; a validator checks the shape of the request; authorization checks whether the trusted staff actor may perform it.

The repository interfaces and unit-of-work interface are promises about what a future storage adapter must provide. They do not implement a database. Similarly, the current-staff, account-eligibility, and first-owner-gate interfaces describe checks a future trusted host must supply. No Infrastructure, API, or Angular implementation was started as part of this work.

## Domain classes and why the office needs them

“Existing, strengthened” means the class was already in the project and gained additional rules or history in this Day 2 follow-up. “Existing” means it was confirmed as part of the current foundation. “New” means it was added in this follow-up.

| Class or type | Status | Business purpose |
|---|---|---|
| `StaffMember` | Existing, strengthened | Represents an employee linked to a user account, their roles, and whether they are invited, active, suspended, or closed. It protects role changes for closed staff, supports reinstatement from suspension, and rejects backward-moving status timestamps. |
| `StaffMemberId` | Existing | Gives each staff record a distinct identifier so it is not confused with a user account, queue, or work item. |
| `StaffRole` | Existing | Names responsibilities such as Operations Staff, Compliance Reviewer, Credit Reviewer, Finance Operator, Servicing Operator, Support Agent, and Application Owner. |
| `StaffStatus` | Existing | Describes whether a staff member may currently take work. |
| `WorkItem` | Existing, strengthened | Represents a task the office must perform, including its type, subject, priority, status, queue, assignee, and histories. It now supports returning work to a queue, transfer, cancellation, escalation resolution, enum validation, and monotonic change times. |
| `WorkItemId` | Existing | Identifies one operational task independently from the subject or staff member. |
| `WorkItemType`, `WorkItemPriority`, `WorkItemStatus` | Existing | Classify the kind, urgency, and lifecycle state of an office task. The Domain now rejects unsupported priority values where priority changes are made. |
| `WorkAssignment` | Existing | Preserves the staff member and time for each assignment, including reassignment history. |
| `WorkTransfer` | New | Records a queue-to-queue handover, its time, and the reason, so the work’s route can be reconstructed. |
| `WorkEscalation` | Existing, strengthened | Preserves escalation reasons and times; its record can also hold optional responsibility and resolution details. The current escalation command does not yet assign a responsible staff member explicitly. |
| `WorkQueueDefinition` | Existing | Represents a team queue, its type, name, and members. |
| `WorkQueueId` and `WorkQueueType` | Existing | Identify queues and the teams they represent: Operations, Compliance, Credit Review, Finance, Servicing, and Support. |
| `PermissionCode` | Existing | Gives protected actions stable names, such as managing staff or work, reviewing compliance, and viewing audit history. |
| `AuditEvent` | Existing, strengthened | Records an action, actor, subject, time, and bounded details. Construction now validates required values and copies details into a read-only collection. |
| `VettingCase` | Existing, strengthened | Owns a participant review’s lifecycle. It now checks that a decision matches its case subject and assigned reviewer, requires an assigned reviewer to start review, and retains applied decisions in history. |
| `VettingDecision` | Existing, strengthened | Represents an immutable review outcome with its reason, reviewer, policy version, subject, and timestamp. Its creation uses the details validation below. |
| `VettingDecisionDetails` | Existing, strengthened | Validates required identifiers, supported subject/outcome values, reason, policy version, and a valid UTC decision time. |
| `EvidenceItem` | Existing, strengthened | Tracks requested, submitted, and reviewed evidence metadata. It checks required request values and rejects backward-moving or invalid timestamps. It stores no document contents. |
| `EvidenceRequest`, `EvidenceSubmission`, `EvidenceRejection` | Existing | Group the information needed to request, submit, or reject an evidence item. |
| `EvidenceItemId`, `EvidenceType`, `EvidenceStatus` | Existing | Identify evidence and describe its category and review state. |
| `StorageReference` | Existing | Represents an opaque pointer to protected storage rather than storing a document or exposing a public file URL. |
| `DecisionOutcome`, `DecisionReason`, `PolicyVersion`, `VettingSubjectType`, `VettingCaseStatus` | Existing | Give review decisions, explanations, policy references, subjects, and case states explicit business meanings. |
| `UtcTimestamp` | Existing | Provides a value type for timestamps that must be UTC. |

Other Domain areas already present in the repository—identity, borrower profiles, credit assessment, payments, and notifications—were not expanded into new Day 2 workflows. They remain part of the broader product foundation, not the focus of this office-operations work. The borrower-registration use case is useful background because it will eventually create internal intake work, but today’s goal is to prepare the staff and office process that will handle work once it arrives.

## Application classes, commands, and contracts

### Staff setup and control

| Class or type | Status | Office action or reason |
|---|---|---|
| `InviteStaffMemberCommand`, validator, and handler | Existing, strengthened | Invites a person with one or more roles. The handler now checks the account-eligibility contract and asks the staff repository to detect an existing membership before creating another. |
| `StaffStatusHandler<TCommand>` | Existing, strengthened | Shares the load, lifecycle transition, assignment safeguard, audit, and save steps used by staff status commands. |
| `ActivateStaffMemberCommand`, validator, and handler | Existing, strengthened | Activates an invited staff member so they can participate in office work. |
| `SuspendStaffMemberCommand`, validator, and handler | Existing, strengthened | Suspends active staff. The application protects the last active owner and refuses the transition while active work remains assigned to that person. |
| `CloseStaffMemberCommand`, validator, and handler | Existing, strengthened | Closes a staff relationship subject to assignment and last-owner safeguards. |
| `AddStaffRoleCommand`, validator, and handler | New | Adds an allowed role and stages an audit record for the change. |
| `RemoveStaffRoleCommand`, validator, and handler | New | Removes a role, protects the last active owner, and requires outstanding work to be handed over first. |
| `ReinstateStaffMemberCommand`, validator, and handler | New | Returns suspended staff to active status. The current handler saves the status change; audit coverage for reinstatement remains to be completed. |
| `BootstrapFirstOwnerCommand`, validator, and handler | New | Creates and activates the initial Application Owner only after a trusted gate approves, no owner membership has previously existed, and the account is eligible. |
| `ICurrentStaffActor` | New | Supplies the actor identity established by a trusted outer boundary. The authorization pipeline compares it with the actor named on an authorized command. |
| `IStaffAccountEligibility` | New | Defines the check a future host/adapter must perform before an account can become staff. |
| `IFirstOwnerBootstrapGate` | New | Defines the controlled, one-time authorization required to bootstrap the first owner; it is not an open public bypass. |
| `IStaffMemberRepository` | Existing, expanded | Describes adding and retrieving staff plus duplicate-account and owner-count checks required by the workflows. It has no persistence implementation here. |

### Queues and work routing

| Class or type | Status | Office action or reason |
|---|---|---|
| `CreateWorkQueueCommand`, validator, and handler | New | Creates a named team queue and stages an audit entry. |
| `QueueRolePolicy` | New | Defines which staff roles may join each queue and which queue types can receive each work type. Application Owners may join any queue. |
| `AddQueueMemberCommand`, validator, and handler | Existing, strengthened | Adds only active staff with a role suitable for that queue, then stages an audit entry. |
| `RemoveQueueMemberCommand`, validator, and handler | Existing, strengthened | Refuses to remove a member while active work remains assigned to them in that queue, preventing silent orphaning. |
| `IWorkQueueRepository` | Existing, expanded | Defines queue creation and lookup for queue setup and routing. No adapter is implemented. |
| `OpenWorkItemCommand`, validator, and handler | Existing, strengthened | Opens work only in a queue eligible for that work type and checks for an already-active item with the same type and subject. |
| `OpenBorrowerRegistrationWorkItemCommand`, validator, and handler | Existing, strengthened | Opens a synthetic internal intake task only in an Operations queue and rejects duplicate active intake for the same profile. This prepares office intake; it does not add borrower registration functionality. |
| `IWorkItemRepository` | Existing, expanded | Defines work lookups plus active-duplicate and outstanding-assignment checks. Adapters must enforce uniqueness under concurrent requests. |

### Borrower-registration context already in the repository

These classes provide background for why the office needs an intake process. They are not the workstream being advanced in this Day 2 internal-operations follow-up.

| Class or type | Business purpose |
|---|---|
| `RegisterBorrowerCommand` and validator | Describe and validate the information supplied to the existing borrower-registration use case. |
| `RegisterBorrowerHandler` | Coordinates account, profile, consent, duplicate checks, and unit-of-work save through Application abstractions. |
| `RegisterBorrowerResult` | Returns the created borrower profile identifier and state to the caller. |
| `BorrowerProfile`, `BorrowerProfileCreation`, and `BorrowerProfileId` | Represent the borrower’s business profile separately from their user account and define its lifecycle. |
| `PersonalName`, `ConsentRecord`, and `ProfileState` | Validate profile name, retain the consent decision/version, and identify profile readiness. |
| `IUserAccountRepository` and `IBorrowerProfileRepository` | Describe the Application’s required account/profile operations without implementing storage. |

### Work ownership and lifecycle

| Class or type | Status | Office action or reason |
|---|---|---|
| `AssignWorkItemCommand`, validator, and handler | Existing, strengthened | Assigns only to active staff who belong to the owning queue and still have an eligible role. Assignment history is kept. |
| `CompleteWorkItemCommand`, validator, and handler | Existing | Completes in-progress work. Specific completion outcomes and decision references still need to be defined. |
| `EscalateWorkItemCommand`, validator, and handler | Existing, strengthened | Raises priority to urgent, retains the escalation reason/history, and stages an audit entry. |
| `UnassignWorkItemCommand`, validator, and handler | New | Returns assigned work to its queue so another staff member can take it. |
| `TransferWorkItemCommand`, validator, and handler | New | Checks that the target queue can receive the work, clears the current assignment, and retains the transfer reason and history. |
| `CancelWorkItemCommand`, validator, and handler | New | Cancels active work only with a reason and records the change. |
| `ChangeWorkItemPriorityCommand`, validator, and handler | New | Changes the priority through an authorized command and rejects unsupported priority values in the Domain. |
| `ResolveWorkItemEscalationCommand`, validator, and handler | New | Resolves an open escalation while keeping who resolved it, when, and why. |

### Permission, validation, audit, and saving patterns

| Class or type | Status | Pattern and business reason |
|---|---|---|
| `StaffPermissionPolicy` | New | Central role-to-permission map. Application Owners receive all listed permissions; operational roles receive permissions aligned to their work. |
| `AuthorizationBehavior<TRequest,TResponse>` | Existing, strengthened | MediatR pipeline behavior for protected commands. It verifies the trusted actor matches the command, loads staff, requires Active status and a permitted role, then consults `IAuthorizationService`. It can return the failure shape for both `DomainResult` and `DomainResult<T>`. |
| `ValidationBehavior<TRequest,TResponse>` | Existing, strengthened | MediatR pipeline behavior that runs FluentValidation validators and converts failures into either result shape consistently. |
| `OperationalAudit` | New | Shared Application helper that builds a validated audit event, appends it through `IAuditEventWriter`, and then calls the unit of work. |
| `IAuditEventWriter` | Existing | Port for staging audit entries. The contract says implementations should stage records in the same unit of work; actual atomic storage remains an adapter responsibility. |
| `IUnitOfWork` | Existing | Port for saving the operation. Its contract states the adapter should commit everything atomically or commit nothing; this has not been proven against a database. |
| `IAuthorizationService` | Existing | Port for the configured permission decision in addition to the in-application staff/role check. No authentication-provider integration was added. |
| `DependencyInjection.AddClearLendApplication` | Existing, updated | Registers MediatR handlers, FluentValidation validators, and the validation and authorization pipeline behaviors. |
| `DomainResult`, `DomainResult<T>`, `DomainError`, `DomainValidationError` | Existing | Represent expected business and validation failures as typed outcomes rather than using exceptions for ordinary rejection cases. |
| `TimeProvider` injection | Existing pattern, used by new handlers | Lets Application workflows use a controllable clock in tests rather than hard-coding current time. Identifiers still use the existing GUID generation unless a caller supplies one. |

## Patterns used and why

- **Domain model with named behavior:** entities such as `StaffMember`, `WorkItem`, and `VettingCase` own their state changes. Application handlers coordinate operations, but the Domain decides whether a transition is valid.
- **Typed identifiers:** staff, work, queue, case, evidence, and account IDs are distinct types, reducing accidental mix-ups.
- **Enums and validated details:** roles, statuses, priorities, queue types, outcomes, and policy references use explicit types and checks rather than arbitrary strings wherever the model already supports that distinction.
- **Command/handler use cases:** each office action has a request, optional FluentValidation validator, and MediatR handler. This gives actions a consistent place for authorization, orchestration, audit, and saving.
- **Pipeline behaviors:** cross-cutting validation and authorization run around handlers so individual handlers do not need to duplicate those checks.
- **Ports and adapters:** repository, authorization, actor, account-eligibility, audit, bootstrap, and unit-of-work interfaces define what the Application needs without implementing Infrastructure.
- **History instead of overwriting:** assignments, escalations, transfers, and vetting decisions preserve important changes so the office can explain what happened.
- **Audit as business evidence:** audit entries are separate from diagnostic logging and use bounded references/details. The future storage adapter must make the audit entry and business change durable together.
- **Explicit time and result values:** `TimeProvider` supports controllable Application timestamps; `DomainResult` describes expected business rejection clearly.
- **Test doubles:** the new pipeline tests use in-memory service registrations to exercise authorization dispatch without a database. The full end-to-end in-memory office simulation has not yet been added.

## Business rules and assumptions recorded today

- New registration intake belongs in an Operations queue.
- Vetting work may go to Operations or Compliance; Credit Review, Finance, Servicing, and Support work go to the matching queue type.
- Queue membership requires an active staff member with a role suitable for the queue. Application Owners can join every queue.
- Staff with active assignments must return or transfer that work before suspension, closure, or role removal. Queue membership cannot be removed while that member still owns active queue work.
- The last active Application Owner cannot be suspended, closed, or stripped of the owner role through the implemented workflows. The first owner requires the future trusted bootstrap gate.
- The assigned reviewer cannot be the subject of the case. Broader conflict-of-interest rules and role eligibility for reviewers remain to be designed and enforced by Application workflows.
- Repository uniqueness checks and atomic audit persistence are contracts for future adapters. Application-level prechecks alone cannot guarantee behavior under concurrent requests.

## What was already present and what this follow-up changed

The repository already had the core staff lifecycle, work queues, membership management, work opening/assignment/completion/escalation, typed permission codes, pipeline behaviors, and the vetting/evidence Domain models. This follow-up did not create those foundations from scratch. It strengthened them and added role commands, reinstatement, first-owner bootstrap contracts, queue creation and policies, more work-item transitions, audit construction/staging, trusted actor checking, and additional validation/history rules.

No Infrastructure, EF Core, SQL Server, authentication provider, API endpoint, Angular application, external service, or borrower-facing registration flow was implemented as part of this Day 2 follow-up.

## Current position and remaining work

The Domain and Application projects compile. The test projects compile. We cannot claim the tests passed because the test runner hangs after discovering the test assembly and reports no test outcomes.

The internal office is not yet ready to open. Remaining Domain/Application work includes:

- Build reviewer assignment, review start/resume, information-request, evidence review, and decision-recording Application workflows, including staff-role eligibility and connection to work items.
- Add reason/history for case review actions where needed and define evidence sufficiency requirements with the business before enforcing them.
- Add bounded, permission-checked office queries for staff, roles, queues, membership, unassigned work, an employee’s work, item history, escalations, case/decision history, and audit history.
- Enforce resource-level permissions for cross-staff assignment, completion, escalation, queue administration, and review actions. The current permission policy does not decide every resource-specific case.
- Complete audit coverage for all significant actions, including reinstatement and every review transition, and test failure behavior using doubles.
- Add a complete in-memory acceptance scenario from first-owner bootstrap to history retrieval, including unauthorized and invalid variants.
- Fix or work around the test-runner environment and obtain executed test results.
- Later, implement trusted actor, account eligibility, bootstrap, repository uniqueness/concurrency, audit storage, and atomic unit-of-work adapters. Those tasks belong to a later layer and are not included here.

## Verification recorded for Day 2

- `dotnet build src/ClearLend.Domain/ClearLend.Domain.csproj --no-restore --verbosity quiet -m:1 -p:UseSharedCompilation=false` — passed with zero warnings and zero errors.
- `dotnet build src/ClearLend.Application/ClearLend.Application.csproj --no-restore --verbosity quiet -m:1 -p:UseSharedCompilation=false` — passed with zero warnings and zero errors.
- `dotnet build tests/ClearLend.Domain.Tests/ClearLend.Domain.Tests.csproj --no-restore --verbosity quiet -m:1 -p:BuildProjectReferences=false -p:UseSharedCompilation=false` — passed with zero warnings and zero errors.
- `dotnet build tests/ClearLend.Application.Tests/ClearLend.Application.Tests.csproj --no-restore --verbosity quiet -m:1 -p:BuildProjectReferences=false -p:UseSharedCompilation=false` — passed with zero warnings and zero errors.
- Domain test execution command: `dotnet test tests/ClearLend.Domain.Tests/ClearLend.Domain.Tests.csproj --no-build --no-restore --verbosity normal -m:1 --logger "console;verbosity=detailed" -- RunConfiguration.TestSessionTimeout=15000` — test assembly was discovered, but no test results appeared before the 15-second session timeout aborted the run.
- Application test execution command: `dotnet test tests/ClearLend.Application.Tests/ClearLend.Application.Tests.csproj --no-build --no-restore --verbosity minimal -m:1 -- RunConfiguration.TestSessionTimeout=15000` — same discovery-then-timeout behavior, with no test results.

Compilation confirms that the projects build. It does not confirm that the tests executed or that the office acceptance journey passes.
