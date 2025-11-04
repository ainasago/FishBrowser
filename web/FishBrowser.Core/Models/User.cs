using System;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 用户模型 - 用于管理员认证
/// </summary>
public class User
{
    public int Id { get; set; }
    
    /// <summary>
    /// 用户名（唯一）
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 邮箱（唯一）
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希（BCrypt）
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 角色：Admin, User
    /// </summary>
    public string Role { get; set; } = "User";
    
    /// <summary>
    /// 账户是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// 登录失败次数（用于账户锁定）
    /// </summary>
    public int LoginFailedCount { get; set; } = 0;
    
    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTime? LockoutEndTime { get; set; }
}
