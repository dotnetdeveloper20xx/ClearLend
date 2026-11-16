using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Identity.Register;

public sealed record RegisterUserCommand(string Email) : IRequest<DomainResult<RegisterUserResult>>;
