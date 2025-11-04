using System;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.Api.Configuration;
using FishBrowser.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Services;

public class AuthService : IAuthService
{
    private readonly WebScraperDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        WebScraperDbContext context,
        ITokenService tokenService,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
                return null;
            }

            // 检查账户是否激活
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account is inactive - {Username}", request.Username);
                throw new Exception("账户已被禁用");
            }

            // 检查账户锁定
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
            {
                _logger.LogWarning("Login failed: Account is locked - {Username}", request.Username);
                throw new Exception($"账户已锁定，请在 {user.LockoutEndTime:yyyy-MM-dd HH:mm:ss} 后重试");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.LoginFailedCount++;
                if (user.LoginFailedCount >= 5)
                {
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(15);
                    _logger.LogWarning("Account locked due to too many failed attempts - {Username}", request.Username);
                }
                await _context.SaveChangesAsync();
                
                _logger.LogWarning("Login failed: Invalid password - {Username}", request.Username);
                return null;
            }

            // 登录成功，重置失败计数
            user.LoginFailedCount = 0;
            user.LockoutEndTime = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 生成 Token
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // 保存 Refresh Token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in successfully - {Username}", request.Username);

            return new LoginResponse
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Username}", request.Username);
            throw;
        }
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (tokenEntity == null || !tokenEntity.IsActive)
            {
                _logger.LogWarning("Refresh token is invalid or expired");
                return null;
            }

            var user = await _context.Users.FindAsync(tokenEntity.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive for refresh token");
                return null;
            }

            // 撤销旧的 Refresh Token
            tokenEntity.RevokedAt = DateTime.UtcNow;

            // 生成新的 Token
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // 保存新的 Refresh Token
            var newTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(newTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return new RefreshTokenResponse
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (tokenEntity == null)
            {
                return false;
            }

            tokenEntity.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token revoked successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    public async Task<User?> ValidateTokenAsync(string token)
    {
        var principal = _tokenService.ValidateToken(token);
        if (principal == null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }

        return await _context.Users.FindAsync(userId);
    }
}
