namespace FishBrowser.Api.DTOs.Auth;

public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string? Error { get; set; }
}
