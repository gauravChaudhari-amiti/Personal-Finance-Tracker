using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? accountId,
        [FromQuery] string? categoryId,
        [FromQuery] string? type)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_reportService.GetReport(userId, from, to, accountId, categoryId, type));
    }

    [HttpGet("export/csv")]
    public IActionResult ExportCsv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? accountId,
        [FromQuery] string? categoryId,
        [FromQuery] string? type)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var csv = _reportService.ExportCsv(userId, from, to, accountId, categoryId, type);
        var fileName = $"finance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
}
