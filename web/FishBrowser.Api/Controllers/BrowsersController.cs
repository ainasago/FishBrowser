using FishBrowser.Api.DTOs.Browser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BrowsersController : ControllerBase
{
    private readonly WebScraperDbContext _context;
    private readonly ILogger<BrowsersController> _logger;

    public BrowsersController(
        WebScraperDbContext context,
        ILogger<BrowsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取浏览器列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BrowserDto>>> GetBrowsers([FromQuery] BrowserQueryParams queryParams)
    {
        try
        {
            var query = _context.BrowserEnvironments
                .Include(b => b.Group)
                .Include(b => b.FingerprintProfile)
                .AsQueryable();

            // 过滤
            if (queryParams.GroupId.HasValue)
            {
                if (queryParams.GroupId == -1)
                    query = query.Where(b => b.GroupId == null);
                else
                    query = query.Where(b => b.GroupId == queryParams.GroupId);
            }

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var search = queryParams.Search.ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(search) ||
                    (b.UserAgent != null && b.UserAgent.ToLower().Contains(search)));
            }

            // 排序
            query = queryParams.SortBy.ToLower() switch
            {
                "createdat" => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt),
                "launchcount" => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.LaunchCount)
                    : query.OrderBy(b => b.LaunchCount),
                _ => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.Name)
                    : query.OrderBy(b => b.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(b => new BrowserDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    GroupId = b.GroupId,
                    GroupName = b.Group != null ? b.Group.Name : null,
                    Engine = b.Engine,
                    OS = b.OS,
                    UserAgent = b.UserAgent,
                    LaunchCount = b.LaunchCount,
                    EnablePersistence = b.EnablePersistence,
                    CreatedAt = b.CreatedAt,
                    LastLaunchedAt = b.LastLaunchedAt
                })
                .ToListAsync();

            return Ok(new PagedResult<BrowserDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browsers");
            return StatusCode(500, new { error = "获取浏览器列表失败" });
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        try
        {
            var totalCount = await _context.BrowserEnvironments.CountAsync();
            var groupCount = await _context.BrowserGroups.CountAsync();
            var runningCount = 0; // TODO: 实现运行状态检测

            return Ok(new
            {
                totalCount,
                runningCount,
                groupCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { error = "获取统计信息失败" });
        }
    }

    /// <summary>
    /// 删除浏览器
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetBrowser(int id)
    {
        try
        {
            var browser = await _context.BrowserEnvironments
                .Include(b => b.Group)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = browser.Id,
                    name = browser.Name,
                    groupId = browser.GroupId,
                    groupName = browser.Group?.Name,
                    engine = browser.Engine,
                    os = browser.OS,
                    userAgent = browser.UserAgent,
                    proxyMode = browser.ProxyMode,
                    proxyApiUrl = browser.ProxyApiUrl,
                    launchCount = browser.LaunchCount,
                    createdAt = browser.CreatedAt,
                    lastLaunchedAt = browser.LastLaunchedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateBrowser(int id, [FromBody] UpdateBrowserRequest request)
    {
        try
        {
            var browser = await _context.BrowserEnvironments.FindAsync(id);
            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            // 更新基本信息
            if (!string.IsNullOrEmpty(request.Name))
                browser.Name = request.Name;
            
            if (request.GroupId.HasValue)
                browser.GroupId = request.GroupId.Value;
            
            if (!string.IsNullOrEmpty(request.UserAgent))
                browser.UserAgent = request.UserAgent;
            
            if (!string.IsNullOrEmpty(request.ProxyMode))
                browser.ProxyMode = request.ProxyMode;
            
            if (!string.IsNullOrEmpty(request.ProxyApiUrl))
                browser.ProxyApiUrl = request.ProxyApiUrl;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "浏览器已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("{id}/launch")]
    public async Task<ActionResult> LaunchBrowser(int id)
    {
        try
        {
            var browser = await _context.BrowserEnvironments.FindAsync(id);
            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            // 更新启动次数和时间
            browser.LaunchCount++;
            browser.LastLaunchedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Browser {Id} launched, count: {Count}", id, browser.LaunchCount);

            return Ok(new
            {
                success = true,
                message = "浏览器启动成功",
                data = new
                {
                    id = browser.Id,
                    name = browser.Name,
                    launchCount = browser.LaunchCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBrowser(int id)
    {
        try
        {
            var browser = await _context.BrowserEnvironments.FindAsync(id);
            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            _context.BrowserEnvironments.Remove(browser);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "浏览器已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class UpdateBrowserRequest
{
    public string? Name { get; set; }
    public int? GroupId { get; set; }
    public string? UserAgent { get; set; }
    public string? ProxyMode { get; set; }
    public string? ProxyApiUrl { get; set; }
}
