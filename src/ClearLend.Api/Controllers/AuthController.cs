using ClearLend.Application.Identity.Login;
using ClearLend.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (!result.IsSuccess)
            return Unauthorized(new { code = result.Error!.Code, message = result.Error.Message });

        var login = result.Value!;
        return Ok(new LoginResponse(login.AccessToken, login.ExpiresInSeconds));
    }
}
