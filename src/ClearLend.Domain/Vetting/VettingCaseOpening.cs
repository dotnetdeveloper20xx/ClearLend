using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed record VettingCaseOpening(
    UserAccountId SubjectAccountId,
    VettingSubjectType SubjectType,
    DateTimeOffset OpenedAt);
