namespace FishBrowser.Api.Configuration;

/// <summary>
/// JWT 配置设置
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// 密钥（至少32字符）
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 发行者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌过期时间（分钟）
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 1440; // 24小时

    /// <summary>
    /// 刷新令牌过期时间（天）
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
