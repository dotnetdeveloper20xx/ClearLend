using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Common;

namespace ClearLend.Domain.Credit;

public enum CreditAssessmentStatus { Requested = 1, Processing = 2, Completed = 3, Failed = 4, Cancelled = 5 }

public readonly record struct CreditAssessmentRequestId(Guid Value)
{
    public static CreditAssessmentRequestId New() => new(Guid.NewGuid());
}

public readonly record struct CreditAssessmentResultId(Guid Value)
{
    public static CreditAssessmentResultId New() => new(Guid.NewGuid());
}

public sealed class CreditAssessmentRequest
{
    private CreditAssessmentRequest(CreditAssessmentRequestId id, BorrowerProfileId borrowerId, DateTimeOffset requestedAt)
    { Id = id; BorrowerId = borrowerId; RequestedAt = requestedAt; Status = CreditAssessmentStatus.Requested; }
    public CreditAssessmentRequestId Id { get; }
    public BorrowerProfileId BorrowerId { get; }
    public CreditAssessmentStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public CreditAssessmentResult? Result { get; private set; }

    public static DomainResult<CreditAssessmentRequest> Request(BorrowerProfileId borrowerId, DateTimeOffset requestedAt, CreditAssessmentRequestId? id = null)
    {
        if (borrowerId.Value == Guid.Empty) return DomainResults.Failure<CreditAssessmentRequest>(new("credit.borrower.required", "A borrower profile is required."));
        if (requestedAt.Offset != TimeSpan.Zero) return DomainResults.Failure<CreditAssessmentRequest>(new("credit.requested_at.not_utc", "Request time must be expressed in UTC."));
        return DomainResults.Success(new CreditAssessmentRequest(id ?? CreditAssessmentRequestId.New(), borrowerId, requestedAt));
    }
    public DomainResult Start(DateTimeOffset at)
    {
        if (Status != CreditAssessmentStatus.Requested) return DomainResult.Failure(new("credit.start.invalid_status", "Only a requested assessment can start processing."));
        return Transition(CreditAssessmentStatus.Processing, at, "credit.start.invalid_status");
    }
    public DomainResult Complete(CreditAssessmentResult result, DateTimeOffset at)
    {
        if (Status != CreditAssessmentStatus.Processing) return DomainResult.Failure(new("credit.complete.invalid_status", "Only a processing assessment can be completed."));
        if (result is null) return DomainResult.Failure(new("credit.result.required", "An assessment result is required."));
        if (result.RequestId != Id) return DomainResult.Failure(new("credit.result.request_mismatch", "The assessment result does not belong to this request."));
        if (at.Offset != TimeSpan.Zero) return DomainResult.Failure(new("credit.completed_at.not_utc", "Completion time must be expressed in UTC."));
        Result = result; Status = CreditAssessmentStatus.Completed; CompletedAt = at; return DomainResult.Success();
    }
    public DomainResult Fail(DateTimeOffset at) => Transition(CreditAssessmentStatus.Failed, at, "credit.fail.invalid_status");
    private DomainResult Transition(CreditAssessmentStatus next, DateTimeOffset at, string code)
    { if (Status != CreditAssessmentStatus.Requested && Status != CreditAssessmentStatus.Processing) return DomainResult.Failure(new(code, "The assessment is not in a transitionable state.")); if (at.Offset != TimeSpan.Zero) return DomainResult.Failure(new("credit.timestamp.not_utc", "Time must be expressed in UTC.")); Status = next; return DomainResult.Success(); }
}

public sealed record CreditAssessmentResult(CreditAssessmentResultId Id, CreditAssessmentRequestId RequestId, int CreditScore, decimal CreditLimit, string Currency, DateTimeOffset CalculatedAt)
{
    public static DomainResult<CreditAssessmentResult> Create(CreditAssessmentRequestId requestId, int creditScore, decimal creditLimit, string? currency, DateTimeOffset calculatedAt, CreditAssessmentResultId? id = null)
    {
        if (requestId.Value == Guid.Empty) return DomainResults.Failure<CreditAssessmentResult>(new("credit.request.required", "An assessment request is required."));
        if (creditScore < 0) return DomainResults.Failure<CreditAssessmentResult>(new("credit.score.invalid", "Credit score cannot be negative."));
        if (creditLimit < 0) return DomainResults.Failure<CreditAssessmentResult>(new("credit.limit.invalid", "Credit limit cannot be negative."));
        if (string.IsNullOrWhiteSpace(currency)) return DomainResults.Failure<CreditAssessmentResult>(new("credit.currency.required", "Currency is required."));
        if (calculatedAt.Offset != TimeSpan.Zero) return DomainResults.Failure<CreditAssessmentResult>(new("credit.calculated_at.not_utc", "Calculation time must be expressed in UTC."));
        return DomainResults.Success(new CreditAssessmentResult(id ?? CreditAssessmentResultId.New(), requestId, creditScore, decimal.Round(creditLimit, 2), currency.Trim().ToUpperInvariant(), calculatedAt));
    }
}
