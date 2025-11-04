using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

public class GroupsController : Controller
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GroupsController(ILogger<GroupsController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("FishBrowserApi");
        var token = HttpContext.Session.GetString("Token");
        
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }

    public IActionResult Index()
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading groups page");
            return RedirectToAction("Login", "Auth");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("/api/groups");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode, new 
            { 
                success = false, 
                error = "无法从 API 获取分组列表" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups from API");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGroup(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"/api/groups/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group {Id} from API", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/groups", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/groups/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {Id}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.DeleteAsync($"/api/groups/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {Id}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class UpdateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
