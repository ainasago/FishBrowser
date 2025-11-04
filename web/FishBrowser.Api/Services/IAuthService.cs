using FishBrowser.Api.DTOs.Auth;
using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<User?> ValidateTokenAsync(string token);
}
