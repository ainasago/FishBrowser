using System.Security.Claims;
using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
