using FishBrowser.Api.DTOs.Browser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly WebScraperDbContext _context;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(
        WebScraperDbContext context,
        ILogger<GroupsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有分组
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GroupDto>>> GetGroups()
    {
        try
        {
            var groups = await _context.BrowserGroups
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Icon = g.Icon,
                    BrowserCount = g.Environments.Count
                })
                .ToListAsync();

            return Ok(new { success = true, data = groups });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return StatusCode(500, new { success = false, error = "获取分组列表失败" });
        }
    }

    /// <summary>
    /// 获取单个分组
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupDto>> GetGroup(int id)
    {
        try
        {
            var group = await _context.BrowserGroups
                .Where(g => g.Id == id)
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Icon = g.Icon,
                    BrowserCount = g.Environments.Count
                })
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return NotFound(new { success = false, error = "分组不存在" });
            }

            return Ok(new { success = true, data = group });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group {Id}", id);
            return StatusCode(500, new { success = false, error = "获取分组失败" });
        }
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupDto dto)
    {
        try
        {
            var group = new BrowserGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon ?? "fas fa-folder",
                CreatedAt = DateTime.UtcNow
            };

            _context.BrowserGroups.Add(group);
            await _context.SaveChangesAsync();

            var result = new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                Icon = group.Icon,
                BrowserCount = 0
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return StatusCode(500, new { success = false, error = "创建分组失败" });
        }
    }

    /// <summary>
    /// 更新分组
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<GroupDto>> UpdateGroup(int id, [FromBody] UpdateGroupDto dto)
    {
        try
        {
            var group = await _context.BrowserGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { success = false, error = "分组不存在" });
            }

            group.Name = dto.Name ?? group.Name;
            group.Description = dto.Description ?? group.Description;
            group.Icon = dto.Icon ?? group.Icon;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                Icon = group.Icon,
                BrowserCount = await _context.BrowserEnvironments.CountAsync(e => e.GroupId == id)
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {Id}", id);
            return StatusCode(500, new { success = false, error = "更新分组失败" });
        }
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGroup(int id)
    {
        try
        {
            var group = await _context.BrowserGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { success = false, error = "分组不存在" });
            }

            // 检查是否有浏览器在此分组中
            var browserCount = await _context.BrowserEnvironments.CountAsync(e => e.GroupId == id);
            if (browserCount > 0)
            {
                return BadRequest(new { success = false, error = $"分组中还有 {browserCount} 个浏览器，无法删除" });
            }

            _context.BrowserGroups.Remove(group);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "分组删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {Id}", id);
            return StatusCode(500, new { success = false, error = "删除分组失败" });
        }
    }
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class UpdateGroupDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
