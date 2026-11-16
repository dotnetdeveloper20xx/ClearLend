using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Borrowers.Profile;

public sealed record GetMyBorrowerProfileQuery : IRequest<DomainResult<BorrowerProfileResult>>;
