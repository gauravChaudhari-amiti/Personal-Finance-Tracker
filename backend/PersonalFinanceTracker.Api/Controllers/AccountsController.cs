using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_accountService.GetAll(userId));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var account = _accountService.GetById(userId, id);

        if (account is null)
            return NotFound(new { message = "Account not found." });

        return Ok(account);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateAccountRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var account = _accountService.Create(userId, request);
            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateAccountRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var account = _accountService.Update(userId, id, request);

            if (account is null)
                return NotFound(new { message = "Account not found." });

            return Ok(account);
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
            var deleted = _accountService.Delete(userId, id);

            if (!deleted)
                return NotFound(new { message = "Account not found." });

            return Ok(new { message = "Account deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transfer")]
    public IActionResult Transfer([FromBody] TransferRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var success = _accountService.Transfer(userId, request, out var message);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}
