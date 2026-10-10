# ClearLend Development Log — Day 3

**Date:** 10 October 2026  
**Status:** Day 3 implementation recorded; builds pass, but test execution is not yet confirmed.  
**Scope:** Domain and Application layers, their tests, and this log only. No Infrastructure, database adapter, API, identity-provider integration, Angular, or borrower-facing screens were implemented.

## The story for the project owner

Picture a new borrower case arriving at the office. An operations staff member assigns it to an active Compliance Reviewer, and the related office task follows that assignment. The reviewer starts the review and examines the evidence. If something is missing, they ask for it clearly. The case and office task both move into “waiting for information,” but the reviewer remains responsible—so nothing gets lost in the queue.

When the information comes back, the reviewer resumes the case and the same work item becomes active again under that reviewer. They can accept the submitted evidence, while the case remains undecided until they make a separate final decision. If they approve the case, the case is approved and the office task is completed. Throughout the journey, the Application stages an audit entry and requests a save for each successful staff action.

The office rules also cover the other outcomes: **Rejected** completes the review task; **More Information Required** puts it into the waiting state while retaining the reviewer; **Restricted** keeps the task open and assigned for follow-up. Inactive or ineligible reviewers, wrong-case evidence, invalid case states, and unauthorized actions are rejected by the relevant workflows.

The work is implemented in the Domain and Application layers and is described by an in-memory journey test. Both test projects build cleanly. However, the test runner has repeatedly discovered the assemblies and then produced no test results before timing out or being stopped. Therefore, the tests compile, but we cannot yet claim they pass. A real database relationship, durable audit history, and proof that all changes commit atomically are still future Infrastructure work.

## Senior developer reference: classes and why they exist

The tables below summarize the Day 3 classes added or changed. “Port” means an Application-owned contract for a future adapter; it does not mean an adapter or database implementation was created.

### Domain layer

| Class | Responsibility and reason |
|---|---|
| `ReviewerAssignment` | Records the reviewer account, assigning staff account, and assignment time so responsibility changes are attributable. |
| `VettingReviewActivity` | Records who started or resumed review and when, preserving a review activity trail. |
| `VettingReviewAction` | Distinguishes starting a review from resuming after information or suspension. |
| `VettingInformationRequest` | Validates and carries the case, reviewer, requested evidence types, explanation, and UTC request time as one coherent Domain value. |
| `VettingCase` | Owns case lifecycle rules and read-only assignment, review, information-request, and decision histories; prevents invalid transitions and mismatched actors or cases. |
| `EvidenceRequest` | Carries the case, evidence type, requester, explanation, and time needed to create a requested evidence record. |
| `EvidenceItem` | Tracks requested/submitted/reviewed evidence, requester and reviewer attribution, and status changes. Accepting evidence does not decide the whole case. |
| `EvidenceRejection` | Captures the rejection explanation, reviewer, and time as structured Domain data. |
| `WorkItemStatus` | Adds `WaitingForInformation` so blocked work is visible without falsely appearing active or losing ownership. |
| `WorkItem` | Enforces work transitions to and from waiting, preserving its assignee; existing assignment and completion behavior supports the review journey. |

### Application layer

| Class or contract | Responsibility and reason |
|---|---|
| `IVettingCaseRepository` | Loads a case by its typed case ID. It is a future persistence port, not a database implementation. |
| `IEvidenceItemRepository` | Adds requested evidence and loads evidence by typed ID so handlers can coordinate requests and reviews. |
| `IVettingWorkItemRepository` | Defines lookup of the active work item explicitly associated with a vetting case. A future adapter must use a real relationship, never guess from free-form subject text. |
| `AssignVettingReviewerCommand` / `AssignVettingReviewerCommandValidator` | Represents reviewer assignment and rejects missing typed IDs before execution; the command requires `ManageWorkItems`. |
| `AssignVettingReviewerHandler` | Checks case and staff eligibility, records assignment, assigns an explicitly linked available vetting task when the port is supplied, then stages audit and requests save. |
| `StartOrResumeVettingReviewCommand` / `StartOrResumeVettingReviewCommandValidator` | Represents starting or resuming review, validates required IDs, and requires `ReviewCompliance`. |
| `StartOrResumeVettingReviewHandler` | Requires the active assigned Compliance Reviewer; applies the valid case transition and checks/resumes a linked work item consistently. |
| `RequestVettingInformationCommand` / `RequestVettingInformationCommandValidator` | Represents a request for one or more unique evidence types with a bounded explanation; requires `ReviewCompliance`. |
| `RequestVettingInformationHandler` | Creates request/evidence records, transitions the case, moves a valid linked task to waiting while retaining its owner, stages safe audit metadata, and requests save. |
| `AcceptEvidenceCommand` / `AcceptEvidenceCommandValidator` | Represents evidence acceptance with validated IDs and `ReviewCompliance` authorization. |
| `AcceptEvidenceHandler` | Coordinates case/evidence/reviewer checks and records evidence acceptance, audit metadata, and save request without deciding the case. |
| `RejectEvidenceCommand` / `RejectEvidenceCommandValidator` | Represents evidence rejection and requires a bounded rejection explanation and valid IDs. |
| `RejectEvidenceHandler` | Applies a rejection only to submitted evidence for the specified case by its assigned active reviewer; keeps the explanation on the evidence record rather than copying it into audit details. |
| `EvidenceReviewLoader` | Shares evidence/case ownership, case-state, reviewer-role, active-staff, and assigned-reviewer checks between accept and reject handlers. |
| `RecordVettingDecisionCommand` / `RecordVettingDecisionCommandValidator` | Carries the outcome, reason, policy version, case, and actor; validates request shape and requires `ReviewCompliance`. |
| `RecordVettingDecisionHandler` | Creates a decision from the case’s own subject details, enforces reviewer and lifecycle rules, applies the agreed linked-work-item outcome, stages metadata-only audit, and requests save. |
| `OperationalAudit` | Shared Application helper used by workflows to stage an operational event and request a unit-of-work save. |
| `IAuditEventWriter` / `IUnitOfWork` | Existing Application boundaries used to stage audit and request persistence. Atomic durable commit remains an adapter responsibility. |

### Test classes

| Test class | What it protects |
|---|---|
| `VettingCaseTests` | Assignment, review, request, and decision lifecycle behavior and retained histories. |
| `EvidenceItemTests` | Evidence request/submission/review rules and reviewer attribution. |
| `WorkItemTests` | Waiting/resume behavior and preservation of the assigned owner. |
| `AssignVettingReviewerTests` | Assignment success and failure cases, including reviewer eligibility, self-review, duplicate assignment, audit, and save expectations. |
| `StartOrResumeVettingReviewTests` | Starting/resuming, reviewer identity, invalid states, authorization, activity history, audit, and save expectations. |
| `RequestVettingInformationTests` | Request validation, evidence creation, wrong/inactive reviewer, audit safety, and save expectations. |
| `EvidenceReviewCommandsTests` | Evidence accept/reject rules, case mismatch, invalid evidence state, rejection reason, reviewer checks, and audit/save behavior. |
| `RecordVettingDecisionTests` | Outcome-to-work-item mapping, invalid owners/states, no-linked-item behavior, authorization, audit, and save expectations. |
| `InternalReviewJourneyTests` | In-memory rehearsal from assignment through information request, response, resume, evidence acceptance, and final approval; checks case/evidence/work-item state, six audit events, and six save requests. |

## Rules and patterns implemented

| Rule or pattern | How it is used |
|---|---|
| Domain-owned lifecycle | `VettingCase`, `EvidenceItem`, and `WorkItem` validate their state transitions and retain their own histories. |
| Command/handler workflow | MediatR commands represent staff intentions; handlers coordinate Domain objects and Application ports. |
| Request validation | FluentValidation rejects malformed command data before workflow execution. |
| Authorization pipeline | Commands declare their required permission; the existing staff authorization behavior enforces it. Handlers additionally verify active staff, reviewer role, assignment, and resource ownership. |
| Typed identifiers | Case, evidence, account, and staff IDs remain distinct types to reduce accidental cross-use. |
| Explicit association port | Case/work lookup is expressed through `IVettingWorkItemRepository`; production uniqueness and relationship enforcement await an adapter. |
| Audit data minimization | Events record operational action and safe metadata; evidence contents and sensitive free-text explanations are not copied into audit details. |
| Unit-of-work boundary | Each successful workflow requests a save after staging its changes. The in-memory tests cannot prove a database transaction commits all records or rolls all of them back. |

## Day 3 implementation story

1. **Give the case an accountable reviewer.** Added assignment history and a staff workflow that checks an active Compliance Reviewer, prevents self-review and duplicate assignment, and records who assigned whom.
2. **Let only that reviewer begin or resume.** Added review activity history and explicit start/resume paths for open, information-waiting, and suspended cases.
3. **Ask clearly for missing information.** Added validated information requests and requested evidence records. The agreed office policy is to keep the task assigned but mark it as waiting.
4. **Review documents without deciding the borrower’s case.** Added separate accept/reject workflows, reviewer attribution, and a required rejection explanation.
5. **Record the final decision and its office consequence.** Added the decision command and explicit mapping: Approved/Rejected complete; More Information Required waits with the reviewer retained; Restricted stays assigned for follow-up.
6. **Connect the case, work, and audit story.** Application handlers coordinate a linked work item through the explicit repository port, and an in-memory journey test describes the normal staff flow end to end.

## Verification and known limitations

- `dotnet build tests/ClearLend.Domain.Tests/ClearLend.Domain.Tests.csproj --no-restore --verbosity minimal -m:1 -p:UseSharedCompilation=false` — passed during the Day 3 slices, 0 warnings and 0 errors.
- `dotnet build tests/ClearLend.Application.Tests/ClearLend.Application.Tests.csproj --no-restore --verbosity minimal -m:1 -p:UseSharedCompilation=false` — passed, including after the integrated journey was added, 0 warnings and 0 errors.
- Focused Domain and Application `dotnet test` runs — assemblies were discovered, but the runner repeatedly returned no test results before configured timeouts; the integrated journey run was stopped after 15 seconds with no result. Tests compile but are **not confirmed passing**.
- `git diff --check` — passed; Git reported expected line-ending conversion warnings for existing modified files.
- No Infrastructure, EF Core, SQL, API, or Angular code was introduced.

Remaining work is to diagnose why the test host stalls and obtain actual test results; implement and verify the explicit case/work-item relationship and its uniqueness in Infrastructure when that layer is authorized; prove atomic persistence of case, work item, evidence, and audit; and add a permission-checked Application history query if office users need to retrieve case/audit history through a read workflow. The in-memory rehearsal is evidence of Application orchestration only, not durable storage or transaction guarantees.
