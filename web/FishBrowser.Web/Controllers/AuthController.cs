using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

public class AuthController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // 如果已登录，跳转到仪表板
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("FishBrowserApi");
            
            var loginData = new
            {
                username = username,
                password = password,
                rememberMe = rememberMe
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
                {
                    // 保存 Token 到 Session
                    HttpContext.Session.SetString("Token", result.Token);
                    HttpContext.Session.SetString("RefreshToken", result.RefreshToken ?? "");
                    HttpContext.Session.SetString("Username", result.User?.Username ?? "");
                    HttpContext.Session.SetString("Role", result.User?.Role ?? "");

                    _logger.LogInformation("User {Username} logged in successfully", username);
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ViewBag.Error = "用户名或密码错误";
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Username}", username);
            ViewBag.Error = "登录失败，请稍后重试";
            return View();
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public UserInfo? User { get; set; }
    public string? Error { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}
