using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public IActionResult GetAll(
        [FromQuery] string? type,
        [FromQuery] string? accountId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_transactionService.GetAll(userId, type, accountId, search, page, pageSize));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var transaction = _transactionService.GetById(userId, id);

        if (transaction is null)
            return NotFound(new { message = "Transaction not found." });

        return Ok(transaction);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateTransactionRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var transaction = _transactionService.Create(userId, request);
            return Ok(transaction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateTransactionRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var transaction = _transactionService.Update(userId, id, request);

            if (transaction is null)
                return NotFound(new { message = "Transaction not found." });

            return Ok(transaction);
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
            var deleted = _transactionService.Delete(userId, id);

            if (!deleted)
                return NotFound(new { message = "Transaction not found." });

            return Ok(new { message = "Transaction deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
