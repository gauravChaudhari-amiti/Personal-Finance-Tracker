using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] int month, [FromQuery] int year)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_budgetService.GetAll(userId, month, year));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateBudgetRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_budgetService.Create(userId, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateBudgetRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var budget = _budgetService.Update(userId, id, request);
            if (budget is null)
                return NotFound(new { message = "Budget not found." });

            return Ok(budget);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var deleted = _budgetService.Delete(userId, id);

        if (!deleted)
            return NotFound(new { message = "Budget not found." });

        return Ok(new { message = "Budget deleted successfully." });
    }

    [HttpPost("duplicate")]
    public IActionResult Duplicate([FromBody] DuplicateBudgetRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_budgetService.Duplicate(userId, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
