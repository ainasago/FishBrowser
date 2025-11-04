using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FishBrowser.Api.DTOs.Auth;
using FishBrowser.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Error = "用户名或密码错误"
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return BadRequest(new LoginResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    Error = "无效的刷新令牌"
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return BadRequest(new RefreshTokenResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken);
            return Ok(new { success = true, message = "登出成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 验证令牌有效性
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public ActionResult Validate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            valid = true,
            user = new { id = userId, username, role }
        });
    }
}
