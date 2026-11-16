using ClearLend.Application.Lenders.Register;
using ClearLend.Contracts.Lenders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController, Route("api/v1/lenders")]
public sealed class LendersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(RegisterLenderRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterLenderCommand(request.Email, request.Password, request.LegalBusinessName, request.TradingName, request.BusinessRegistrationNumber, request.ContactGivenName, request.ContactFamilyName, request.ConsentPolicyVersion), cancellationToken);
        if (!result.IsSuccess) { var error = result.Error!; return StatusCode(error.Code == "identity.email.already_registered" ? 409 : 400, new { code = error.Code, message = error.Message }); }
        var lender = result.Value!;
        return Created($"/api/v1/lenders/{lender.LenderProfileId.Value}", new RegisterLenderResponse(lender.UserAccountId.Value.ToString(), lender.LenderProfileId.Value.ToString(), lender.ProfileState.ToString()));
    }
}
