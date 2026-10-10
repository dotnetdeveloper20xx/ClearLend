using System.Collections.ObjectModel;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;

namespace ClearLend.Domain.Vetting;

public sealed class VettingInformationRequest
{
    private VettingInformationRequest(
        VettingCaseId vettingCaseId,
        UserAccountId requestedByAccountId,
        IReadOnlyList<EvidenceType> requestedEvidenceTypes,
        string reason,
        DateTimeOffset requestedAt)
    {
        VettingCaseId = vettingCaseId;
        RequestedByAccountId = requestedByAccountId;
        RequestedEvidenceTypes = requestedEvidenceTypes;
        Reason = reason;
        RequestedAt = requestedAt;
    }

    public VettingCaseId VettingCaseId { get; }
    public UserAccountId RequestedByAccountId { get; }
    public IReadOnlyList<EvidenceType> RequestedEvidenceTypes { get; }
    public string Reason { get; }
    public DateTimeOffset RequestedAt { get; }

    public static DomainResult<VettingInformationRequest> Create(
        VettingCaseId vettingCaseId,
        UserAccountId requestedByAccountId,
        IEnumerable<EvidenceType>? requestedEvidenceTypes,
        string? reason,
        DateTimeOffset requestedAt)
    {
        if (vettingCaseId.Value == Guid.Empty || requestedByAccountId.Value == Guid.Empty)
            return DomainResults.Failure<VettingInformationRequest>(new("vetting.information_request.identity.invalid", "A valid case and requesting reviewer are required."));
        if (requestedEvidenceTypes is null)
            return DomainResults.Failure<VettingInformationRequest>(new("vetting.information_request.evidence.required", "At least one evidence type must be requested."));

        var types = requestedEvidenceTypes.ToArray();
        if (types.Length == 0 || types.Any(type => !Enum.IsDefined(type)) || types.Distinct().Count() != types.Length)
            return DomainResults.Failure<VettingInformationRequest>(new("vetting.information_request.evidence.invalid", "One or more unique supported evidence types are required."));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return DomainResults.Failure<VettingInformationRequest>(new("vetting.information_request.reason.invalid", "A clear explanation of at most 1000 characters is required."));
        if (requestedAt.Offset != TimeSpan.Zero)
            return DomainResults.Failure<VettingInformationRequest>(new("vetting.information_request.timestamp.not_utc", "The request time must be expressed in UTC."));

        return DomainResults.Success(new VettingInformationRequest(
            vettingCaseId,
            requestedByAccountId,
            new ReadOnlyCollection<EvidenceType>(types),
            reason.Trim(),
            requestedAt));
    }
}
