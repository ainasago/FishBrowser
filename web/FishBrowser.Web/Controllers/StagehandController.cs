using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace FishBrowser.Web.Controllers;

public class StagehandController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public StagehandController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("FishBrowserApi");
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("未登录");
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("api/system/stagehand/status");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Install()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync("api/system/stagehand/install", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { 
                    success = false, 
                    error = $"安装失败: {errorContent}" 
                });
            }
            
            return Ok(new { success = true, message = "安装成功" });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { 
                success = false, 
                error = $"网络请求失败: {ex.Message}" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                success = false, 
                error = $"安装失败: {ex.Message}" 
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync("api/system/stagehand/update", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { 
                    success = false, 
                    error = $"更新失败: {errorContent}" 
                });
            }
            
            return Ok(new { success = true, message = "更新成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                success = false, 
                error = $"更新失败: {ex.Message}" 
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Uninstall()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync("api/system/stagehand/uninstall", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { 
                    success = false, 
                    error = $"卸载失败: {errorContent}" 
                });
            }
            
            return Ok(new { success = true, message = "卸载成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                success = false, 
                error = $"卸载失败: {ex.Message}" 
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Test()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync("api/system/stagehand/test", null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
