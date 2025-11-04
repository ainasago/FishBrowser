using System;
using System.Linq;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Data;

/// <summary>
/// 数据库初始化器 - 创建默认管理员账户
/// </summary>
public static class DbInitializer
{
    public static void Initialize(WebScraperDbContext context)
    {
        try
        {
            // 确保数据库已创建
            context.Database.EnsureCreated();

            // 检查是否已有用户表和数据
            bool hasUsers = false;
            try
            {
                hasUsers = context.Users.Any();
            }
            catch
            {
                // Users 表不存在，需要创建
                hasUsers = false;
            }

            if (hasUsers)
            {
                return; // 数据库已初始化
            }

            // 创建默认管理员账户
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@fishbrowser.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.SaveChanges();

            Console.WriteLine("========================================");
            Console.WriteLine("数据库初始化完成！");
            Console.WriteLine("默认管理员账户：");
            Console.WriteLine("  用户名: admin");
            Console.WriteLine("  密码: Admin@123");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库初始化错误: {ex.Message}");
            // 不抛出异常，让应用继续启动
        }
    }
}
