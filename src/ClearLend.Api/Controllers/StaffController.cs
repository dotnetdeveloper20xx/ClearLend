using ClearLend.Application.Staff.Create;
using ClearLend.Application.Staff.List;
using ClearLend.Application.Staff.Lifecycle;
using ClearLend.Contracts.Staff;
using ClearLend.Domain.Staff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/staff")]
public sealed class StaffController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListStaffQuery(), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized, new { code = result.Error.Code, message = result.Error.Message });
        return Ok(result.Value!.Select(x => new StaffResponse(x.UserAccountId, x.Email, x.Roles.Select(role => role.ToString()).ToArray(), x.Status.ToString(), x.CreatedAt)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateStaffResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var roles = new List<StaffRole>();
        foreach (var role in request.Roles)
        {
            if (!Enum.TryParse<StaffRole>(role, true, out var parsedRole))
                return BadRequest(new { code = "staff.role.invalid", message = $"Unknown staff role '{role}'." });
            roles.Add(parsedRole);
        }

        var result = await sender.Send(new CreateStaffCommand(request.Email, request.Password, roles), cancellationToken);
        if (!result.IsSuccess)
        {
            var status = result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { code = result.Error.Code, message = result.Error.Message });
        }

        var value = result.Value!;
        return Created("/api/v1/staff", new CreateStaffResponse(value.UserAccountId, value.Roles.Select(x => x.ToString()).ToArray()));
    }

    [HttpPatch("{userAccountId}/status")]
    public async Task<IActionResult> ChangeStatus(string userAccountId, ChangeStaffStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeStaffStatusCommand(userAccountId, request.Restore), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest, new { code = result.Error.Code, message = result.Error.Message });
        return NoContent();
    }

    [HttpPost("{userAccountId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userAccountId, ResetStaffPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetStaffPasswordCommand(userAccountId, request.NewPassword), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest, new { code = result.Error.Code, message = result.Error.Message });
        return NoContent();
    }

    [HttpPut("{userAccountId}/roles")]
    public async Task<IActionResult> ChangeRoles(string userAccountId, ChangeStaffRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = new List<StaffRole>();
        foreach (var role in request.Roles)
        {
            if (!Enum.TryParse<StaffRole>(role, true, out var parsed))
                return BadRequest(new { code = "staff.role.invalid", message = $"Unknown staff role '{role}'." });
            roles.Add(parsed);
        }
        var result = await sender.Send(new ChangeStaffRolesCommand(userAccountId, roles), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest, new { code = result.Error.Code, message = result.Error.Message });
        return NoContent();
    }

    [HttpDelete("{userAccountId}")]
    public async Task<IActionResult> Remove(string userAccountId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveStaffCommand(userAccountId), cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.Error!.Code == "staff.authorization.forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest, new { code = result.Error.Code, message = result.Error.Message });
        return NoContent();
    }
}
