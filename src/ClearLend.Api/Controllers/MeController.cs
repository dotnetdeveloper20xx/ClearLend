using ClearLend.Application.Borrowers.Profile;
using ClearLend.Application.Lenders.Profile;
using ClearLend.Contracts.Borrowers;
using ClearLend.Contracts.Lenders;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClearLend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(ISender sender) : ControllerBase
{
    [HttpGet("borrower-profile")]
    [ProducesResponseType(typeof(BorrowerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BorrowerProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyBorrowerProfileQuery(), cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.Code == "auth.required"
                ? Unauthorized(new { code = result.Error.Code, message = result.Error.Message })
                : NotFound(new { code = result.Error.Code, message = result.Error.Message });

        var profile = result.Value!;
        return Ok(new BorrowerProfileResponse(profile.UserAccountId, profile.BorrowerProfileId,
            profile.Email, profile.GivenName, profile.FamilyName, profile.AccountStatus,
            profile.ProfileState, profile.ConsentStatus, profile.RegistrationDate));
    }

    [HttpGet("lender-profile")]
    public async Task<IActionResult> LenderProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyLenderProfileQuery(), cancellationToken);
        if (!result.IsSuccess) return result.Error!.Code == "auth.required" ? Unauthorized(new { code = result.Error.Code, message = result.Error.Message }) : NotFound(new { code = result.Error.Code, message = result.Error.Message });
        var profile = result.Value!;
        return Ok(new LenderProfileResponse(profile.UserAccountId, profile.LenderProfileId, profile.Email, profile.LegalBusinessName, profile.TradingName, profile.BusinessRegistrationNumber, profile.ContactGivenName, profile.ContactFamilyName, profile.AccountStatus, profile.ProfileState, profile.ConsentStatus, profile.RegistrationDate));
    }
}
