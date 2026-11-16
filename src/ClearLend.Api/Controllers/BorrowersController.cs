using ClearLend.Application.Borrowers.Register;
using ClearLend.Contracts.Borrowers;
using ClearLend.Domain.Borrowers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController]
[Route("api/v1/borrowers")]
public sealed class BorrowersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RegisterBorrowerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterBorrowerRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ConsentStatus>(request.ConsentStatus, true, out var consentStatus))
            return BadRequest(new { code = "consent.status.invalid", message = "Consent status must be Granted or Declined." });

        var result = await sender.Send(new RegisterBorrowerCommand(request.Email, request.Password, request.GivenName,
            request.FamilyName, consentStatus, request.ConsentPolicyVersion), cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.Error!;
            return StatusCode(error.Code == "identity.email.already_registered" ? 409 : 400,
                new { code = error.Code, message = error.Message });
        }

        var borrower = result.Value!;
        return Created($"/api/v1/borrowers/{borrower.BorrowerProfileId.Value}",
            new RegisterBorrowerResponse(borrower.UserAccountId.Value.ToString(),
                borrower.BorrowerProfileId.Value.ToString(), borrower.ProfileState.ToString()));
    }
}
