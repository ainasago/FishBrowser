using Microsoft.AspNetCore.Mvc;

namespace FishBrowser.Web.Controllers;

public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // 检查是否已登录
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.Username = username;
        ViewBag.Role = HttpContext.Session.GetString("Role") ?? "管理员";
        
        return View();
    }
}
