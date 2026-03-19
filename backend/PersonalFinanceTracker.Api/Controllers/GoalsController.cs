using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;

    public GoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_goalService.GetAll(userId));
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateGoalRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_goalService.Create(userId, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateGoalRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var goal = _goalService.Update(userId, id, request);
            if (goal is null)
                return NotFound(new { message = "Goal not found." });

            return Ok(goal);
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

        try
        {
            var deleted = _goalService.Delete(userId, id);
            if (!deleted)
                return NotFound(new { message = "Goal not found." });

            return Ok(new { message = "Goal deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/contribute")]
    public IActionResult Contribute(string id, [FromBody] GoalContributionRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_goalService.Contribute(userId, id, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/withdraw")]
    public IActionResult Withdraw(string id, [FromBody] GoalWithdrawalRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_goalService.Withdraw(userId, id, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
