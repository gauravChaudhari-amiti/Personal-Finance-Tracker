using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecurringController : ControllerBase
{
    private readonly IRecurringService _recurringService;

    public RecurringController(IRecurringService recurringService)
    {
        _recurringService = recurringService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_recurringService.GetAll(userId));
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateRecurringTransactionRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_recurringService.Create(userId, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateRecurringTransactionRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var item = _recurringService.Update(userId, id, request);
            if (item is null)
                return NotFound(new { message = "Recurring item not found." });

            return Ok(item);
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
        var deleted = _recurringService.Delete(userId, id);

        if (!deleted)
            return NotFound(new { message = "Recurring item not found." });

        return Ok(new { message = "Recurring item deleted successfully." });
    }
}
