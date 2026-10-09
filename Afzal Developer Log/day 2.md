# ClearLend Development Log — Day 2

**Date:** 9 October 2026  
**Status:** Still in progress  
**Today’s focus:** Build the internal business foundation that will support borrower registration.

## Why we did this work

Yesterday we created the first visible customer journey: a borrower can register, provide their details, and record whether they agree to credit checking.

Today we stepped back and asked: when a borrower registers, who inside ClearLend receives that work, reviews it, makes decisions, and follows it through?

We do not want registration to create a record that simply sits in a database. We want it to start real business work that our staff can own, review, complete, or escalate.

That is why today focused on staff, responsibilities, queues, work items, assignment, permissions, and escalation. We stayed within the Domain and Application layers. The database, API, external credit provider, payment provider, and notification provider are intentionally still waiting for later work.

## The story of what we created

### Staff and roles

We created the `StaffMember` class because ClearLend needs to know who its internal staff are. A staff member is linked to a user account and has a status such as invited, active, suspended, or closed.

This prevents the system from assigning important work to somebody who has not been activated, has been suspended, or has left the business.

We also created the `StaffRole` list. It currently covers Operations Staff, Compliance Reviewer, Credit Reviewer, Finance Operator, Servicing Operator, Support Agent, and Application Owner. These roles describe the type of work a person is allowed to perform.

We added rules so every staff member has at least one valid role. We also added the ability to add or remove roles later, while preventing somebody from being left with no role.

### Work items

We created the `WorkItem` class because a registration, review, exception, or support request needs to become something that can be tracked.

A work item records what kind of work is needed, what it relates to, its priority, its current status, when it was created, and who is handling it. It can be opened, assigned, completed, or escalated.

For borrower registration, this means the registration can become an Operations work item. The borrower profile remains the borrower’s information; the work item represents the internal task our team must carry out.

### Queues

We created `WorkQueueDefinition` because different work belongs with different teams. A registration may begin in Operations, a risk question may go to Compliance, and a credit-limit review may go to Credit Review.

A queue can have staff members as members. We added rules so only active staff can join a queue. This gives us a simple operating model: work enters a queue, an appropriate person takes responsibility, and the work remains traceable.

### Safe assignment

We connected every work item to its queue before it can be assigned. The application now checks that the proposed assignee is active and belongs to that queue.

This prevents work being given to the wrong team or to somebody who is not currently allowed to work.

### Escalation history

We updated escalation so it does more than mark work as urgent. Every escalation keeps its reason and time.

This means a manager can later understand what happened, when the concern appeared, and whether the issue was escalated more than once. Escalation also raises priority to urgent and updates the status-change time.

### Permissions and validation

We introduced permission codes and an authorization pipeline for protected actions. Before staff can open, assign, complete, escalate, or manage queue membership, the request is checked for the required permission.

We kept request validation separate from business rules. Command validators check that incoming data is shaped correctly. The Domain classes make the final business decisions. This keeps the code easier to understand and stops important rules being hidden inside handlers.

## Main classes created or updated

- `StaffMember` — represents an internal employee and controls their lifecycle and roles.
- `StaffRole` and `StaffStatus` — describe responsibilities and whether somebody may work.
- `WorkItem` — represents a piece of internal business work.
- `WorkQueueDefinition` — represents the team queue that owns work.
- `WorkEscalation` — preserves each escalation reason and time.
- `PermissionCode` — gives protected actions consistent permission names.
- Staff commands and handlers — invite, activate, suspend, and close staff.
- Work-item commands and handlers — open, assign, complete, and escalate work.
- Queue-membership commands and handlers — add and remove staff from queues.
- Authorization and validation pipeline behaviours — apply common checks before handlers run.
- Repository and unit-of-work interfaces — describe what Infrastructure will provide later.

We also added and updated tests around role rules, staff lifecycle, escalation history, queue membership, and command validation.

## How this supports borrower registration

The intended journey is now:

```text
Borrower registers
        ↓
Borrower profile and consent are recorded
        ↓
An internal registration work item is opened
        ↓
The work item enters an Operations queue
        ↓
An active Operations staff member takes ownership
        ↓
Compliance and credit work can be routed to the right teams
        ↓
Concerns can be escalated and retained in history
        ↓
The business records a decision and eventually notifies the borrower
```

Only the first part of this journey is fully connected to borrower registration so far. Today’s work creates the internal foundation needed for the rest.

## What the code review found and what we fixed

After the first five phases, we reviewed the work as senior architects rather than assuming that compiling code was automatically good code.

The review found that role values and queue types needed stronger validation. Work items needed explicit queue ownership. Assignment needed to check that the person was active and in the correct queue. Escalation needed history instead of only a current flag. We corrected these issues and added regression tests for the important rules.

The review also confirmed that audit-event generation and complete operational decision workflows still need more design. We have the correct direction and abstractions, but we have not pretended that persistence, Infrastructure, or external integrations are complete.

## Where we stand now

Phases 1–5 are complete within the Domain and Application layers:

1. Staff and roles
2. Internal work management
3. Borrower-registration work
4. Escalation
5. Queues, assignment rules, and authorization foundations

Both Domain and Application projects build successfully with zero warnings and zero errors.

The test runner currently stops after discovering the test assembly in this environment. The test projects compile successfully, but runtime test completion still needs investigation before we claim every test has executed successfully.

## The bigger picture

Today’s work is not the complete ClearLend platform. It is the internal shop floor behind the platform.

Before adding credit scoring, credit limits, payments, fees, notifications, dashboards, and external integrations, ClearLend needs to know who performs the work and how that work moves through the business. The classes created today give us that starting structure.

The next sensible step is to deepen the operational foundation—especially audit history and business decisions—before moving into detailed compliance and vetting workflows.

More work and decisions will be added to this log later today.
