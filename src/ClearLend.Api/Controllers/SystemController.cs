using Microsoft.AspNetCore.Mvc;

namespace ClearLend.Api.Controllers;

[ApiController]
[Route("")]
public sealed class SystemController : ControllerBase
{
    [HttpGet]
    public IActionResult Status() => Ok(new { service = "ClearLend.Api", status = "running" });
}
