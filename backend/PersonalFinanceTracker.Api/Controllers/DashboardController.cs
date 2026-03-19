using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        var result = _dashboardService.GetSummary(userId, role);
        return Ok(result);
    }
}
