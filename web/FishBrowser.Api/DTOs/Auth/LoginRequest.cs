using System.ComponentModel.DataAnnotations;

namespace FishBrowser.Api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
