using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Vetting;

namespace ClearLend.Domain.Borrowers;

public sealed class BorrowerProfile
{
    private BorrowerProfile(
        BorrowerProfileId id,
        UserAccountId userAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserAccountId = userAccountId;
        CreatedAt = createdAt;
        State = ProfileState.Incomplete;
        StateChangedAt = createdAt;
    }

    public BorrowerProfileId Id { get; }

    public UserAccountId UserAccountId { get; }

    public PersonalName? Name { get; private set; }

    public ProfileState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset StateChangedAt { get; private set; }

    public static DomainResult<BorrowerProfile> Create(
        BorrowerProfileCreation? creation,
        BorrowerProfileId? id = null)
    {
        if (creation is null)
        {
            return DomainResults.Failure<BorrowerProfile>(
                new("borrower.profile_creation.required", "Borrower-profile creation details are required."));
        }

        if (creation.CreatedAt.Offset != TimeSpan.Zero)
        {
            return DomainResults.Failure<BorrowerProfile>(
                new("borrower.created_at.not_utc", "Creation time must be expressed in UTC."));
        }

        return DomainResults.Success<BorrowerProfile>(
            new(id ?? BorrowerProfileId.New(), creation.UserAccountId, creation.CreatedAt));
    }

    public DomainResult Complete(PersonalName? name)
    {
        if (State != ProfileState.Incomplete)
        {
            return DomainResult.Failure(
                new("borrower.profile.complete.invalid_state", "Only an incomplete profile can be completed."));
        }

        if (name is null)
        {
            return DomainResult.Failure(
                new("borrower.name.required", "A personal name is required."));
        }

        Name = name;
        return DomainResult.Success();
    }

    public DomainResult SubmitForReview(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (State != ProfileState.Incomplete || Name is null)
        {
            return DomainResult.Failure(
                new("borrower.profile.submit.invalid_state", "A completed profile is required before review submission."));
        }

        TransitionTo(ProfileState.ReadyForReview, changedAt);
        return DomainResult.Success();
    }

    public DomainResult Activate(VettingDecision decision, DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (State != ProfileState.ReadyForReview ||
            decision.Outcome != DecisionOutcome.Approved ||
            decision.SubjectType != VettingSubjectType.Borrower ||
            decision.SubjectAccountId != UserAccountId)
        {
            return DomainResult.Failure(
                new("borrower.profile.activate.invalid_decision", "Only a profile with an approved vetting decision can be activated."));
        }

        TransitionTo(ProfileState.Active, changedAt);
        return DomainResult.Success();
    }

    public DomainResult Suspend(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (State != ProfileState.Active)
        {
            return DomainResult.Failure(
                new("borrower.profile.suspend.invalid_state", "Only an active profile can be suspended."));
        }

        TransitionTo(ProfileState.Suspended, changedAt);
        return DomainResult.Success();
    }

    public DomainResult Close(DateTimeOffset changedAt)
    {
        var validTime = EnsureUtc(changedAt);
        if (!validTime.IsSuccess) return validTime;

        if (State == ProfileState.Closed)
        {
            return DomainResult.Failure(
                new("borrower.profile.already_closed", "The borrower profile is already closed."));
        }

        TransitionTo(ProfileState.Closed, changedAt);
        return DomainResult.Success();
    }

    private void TransitionTo(ProfileState state, DateTimeOffset changedAt)
    {
        State = state;
        StateChangedAt = changedAt;
    }

    private static DomainResult EnsureUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero
            ? DomainResult.Success()
            : DomainResult.Failure(new("borrower.timestamp.not_utc", "Time must be expressed in UTC."));
}
