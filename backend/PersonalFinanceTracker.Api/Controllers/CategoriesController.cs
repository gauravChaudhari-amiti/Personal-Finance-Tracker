using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? type, [FromQuery] bool includeArchived = false)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        return Ok(_categoryService.GetAll(userId, type, includeArchived));
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateCategoryRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            return Ok(_categoryService.Create(userId, request));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateCategoryRequestDto request)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;

        try
        {
            var category = _categoryService.Update(userId, id, request);
            if (category is null)
                return NotFound(new { message = "Category not found." });

            return Ok(category);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/archive")]
    public IActionResult Archive(string id, [FromQuery] bool isArchived = true)
    {
        var userId = User.FindFirst("userId")?.Value ?? string.Empty;
        var category = _categoryService.Archive(userId, id, isArchived);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        return Ok(category);
    }
}
