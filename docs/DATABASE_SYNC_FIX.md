# 数据库表同步问题修复

## 问题描述

在首次启动 API 时，出现以下错误：

```
SQLite Error 1: 'no such table: Users'
```

这是因为数据库中没有 `Users` 和 `RefreshTokens` 表。

## 问题原因

1. EF Core 的 `EnsureCreated()` 方法只会创建 DbContext 中定义的实体表
2. 但是在现有数据库中，`EnsureCreated()` 不会添加新表
3. 需要手动创建新增的认证相关表

## 解决方案

在 `FishBrowser.Api/Program.cs` 中添加了表结构同步代码：

```csharp
// 使用原始 SQL 创建 Users 表（如果不存在）
var connection = context.Database.GetDbConnection();
if (connection.State != System.Data.ConnectionState.Open)
    connection.Open();

using (var command = connection.CreateCommand())
{
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            Email TEXT NOT NULL,
            PasswordHash TEXT NOT NULL,
            Role TEXT NOT NULL DEFAULT 'User',
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            LastLoginAt TEXT,
            LoginFailedCount INTEGER NOT NULL DEFAULT 0,
            LockoutEndTime TEXT
        );
        
        CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users(Username);
        CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email);
        
        CREATE TABLE IF NOT EXISTS RefreshTokens (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Token TEXT NOT NULL,
            ExpiresAt TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            RevokedAt TEXT
        );
        
        CREATE UNIQUE INDEX IF NOT EXISTS IX_RefreshTokens_Token ON RefreshTokens(Token);
        CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId ON RefreshTokens(UserId);
        CREATE INDEX IF NOT EXISTS IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
    ";
    command.ExecuteNonQuery();
}
```

## 表结构说明

### Users 表

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| Username | TEXT | 用户名，唯一 |
| Email | TEXT | 邮箱，唯一 |
| PasswordHash | TEXT | 密码哈希（BCrypt） |
| Role | TEXT | 角色（Admin/User） |
| IsActive | INTEGER | 是否激活（1/0） |
| CreatedAt | TEXT | 创建时间 |
| LastLoginAt | TEXT | 最后登录时间 |
| LoginFailedCount | INTEGER | 登录失败次数 |
| LockoutEndTime | TEXT | 锁定结束时间 |

### RefreshTokens 表

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INTEGER | 主键，自增 |
| UserId | INTEGER | 用户ID |
| Token | TEXT | 刷新令牌，唯一 |
| ExpiresAt | TEXT | 过期时间 |
| CreatedAt | TEXT | 创建时间 |
| RevokedAt | TEXT | 撤销时间 |

## 验证方法

1. **启动 API**
   ```bash
   cd d:\1Dev\webbrowser\web\FishBrowser.Api
   dotnet run
   ```

2. **查看启动日志**
   应该看到：
   ```
   数据库表结构同步完成！
   ========================================
   数据库初始化完成！
   默认管理员账户：
     用户名: admin
     密码: Admin@123
   ========================================
   ```

3. **测试登录**
   访问 http://localhost:5208 并使用 admin/Admin@123 登录

## 未来改进

### 使用 FreeSql 自动同步

参考 `FishBrowser.Core` 中的 `FreeSqlMigrationManager`，可以实现更智能的表结构同步：

```csharp
// 注册 FreeSql
services.AddSingleton<IFreeSql>(sp =>
{
    var fsql = new FreeSqlBuilder()
        .UseConnectionString(DataType.Sqlite, connectionString)
        .UseAutoSyncStructure(true)  // 自动同步表结构
        .Build();
    return fsql;
});

// 使用 FreeSql 同步表结构
var fsql = scope.ServiceProvider.GetRequiredService<IFreeSql>();
fsql.CodeFirst.SyncStructure<User>();
fsql.CodeFirst.SyncStructure<RefreshToken>();
```

### 使用 EF Core Migrations

对于生产环境，建议使用 EF Core Migrations：

```bash
# 创建迁移
dotnet ef migrations add AddAuthTables

# 应用迁移
dotnet ef database update
```

## 相关文件

- `FishBrowser.Api/Program.cs` - 数据库初始化代码
- `FishBrowser.Core/Data/DbInitializer.cs` - 默认数据初始化
- `FishBrowser.Core/Models/User.cs` - 用户模型
- `FishBrowser.Core/Models/RefreshToken.cs` - 刷新令牌模型

## 修复时间

- **问题发现**: 2025-11-03 21:40
- **修复完成**: 2025-11-03 21:42
- **验证通过**: 2025-11-03 21:43

## 状态

✅ 已修复并验证通过

---

**文档版本**: 1.0  
**创建日期**: 2025-11-03  
**最后更新**: 2025-11-03 21:43
