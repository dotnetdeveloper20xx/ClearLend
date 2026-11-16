using ClearLend.Domain.Common;
using MediatR;

namespace ClearLend.Application.Identity.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<DomainResult<LoginResult>>;
