using ClearLend.Application.Identity.Register;
using ClearLend.Contracts.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterUserCommand(request.Email), cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error!;
            return StatusCode(error.Code == "identity.email.already_registered" ? 409 : 400,
                new { code = error.Code, message = error.Message });
        }

        var user = result.Value!;
        return Created($"/api/v1/users/{user.UserAccountId.Value}",
            new RegisterUserResponse(user.UserAccountId.Value.ToString(), user.Email, user.Status.ToString()));
    }
}
