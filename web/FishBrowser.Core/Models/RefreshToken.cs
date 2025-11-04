using System;

namespace FishBrowser.WPF.Models;

/// <summary>
/// Refresh Token 模型 - 用于刷新访问令牌
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    
    /// <summary>
    /// 关联的用户ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Token 字符串（唯一）
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 撤销时间（如果已撤销）
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked => RevokedAt != null;
    
    /// <summary>
    /// 是否有效（未过期且未撤销）
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
