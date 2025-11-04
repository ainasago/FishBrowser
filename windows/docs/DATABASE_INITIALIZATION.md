# 数据库初始化分析

## 问题发现

程序中有 **3 处地方** 初始化数据库，存在重复和冲突：

## 📍 初始化位置

### 1. Program.cs (WPF 主程序) ❌ 有问题

**文件**: `Program.cs` (第 76-95 行)

```csharp
// 初始化数据库
using (var scope = Host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WebScraperDbContext>();
    try
    {
        Console.WriteLine("Starting database initialization...");
        // 删除旧数据库并重新创建
        var deleted = dbContext.Database.EnsureDeleted();
        Console.WriteLine($"Database deleted: {deleted}");
        var created = dbContext.Database.EnsureCreated();
        Console.WriteLine($"Database created: {created}");
        Console.WriteLine("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}
```

**问题**:
- ❌ 每次启动都删除数据库 (`EnsureDeleted()`)
- ❌ 会丢失所有数据
- ❌ 与 App.xaml.cs 中的初始化冲突

---

### 2. App.xaml.cs (WPF 应用启动) ✅ 正确

**文件**: `App.xaml.cs` (第 56-72 行)

```csharp
// 初始化数据库 (使用 FreeSql 迁移管理器，不删除数据)
using (var scope = Host.Services.CreateScope())
{
    try
    {
        var migrationManager = scope.ServiceProvider.GetRequiredService<FreeSqlMigrationManager>();
        migrationManager.InitializeDatabase();

        // 显示数据库统计信息
        var stats = migrationManager.GetStatistics();
        Console.WriteLine($"Database initialized: {stats.TableCount} tables, Size: {stats.GetFormattedSize()}");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Database initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

**优点**:
- ✅ 使用 `FreeSqlMigrationManager`
- ✅ 自动同步表结构
- ✅ 不删除现有数据
- ✅ 显示初始化统计信息

---

### 3. Program.CLI.cs (CLI 程序) ⚠️ 简单

**文件**: `Program.CLI.cs` (第 52-57 行)

```csharp
// 初始化数据库
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WebScraperDbContext>();
    dbContext.Database.EnsureCreated();
}
```

**特点**:
- ⚠️ 仅创建数据库，不删除
- ⚠️ 没有使用 `FreeSqlMigrationManager`
- ⚠️ 没有错误处理
- ⚠️ 没有初始化统计信息

---

## 🔧 建议修复

### 方案：统一使用 FreeSqlMigrationManager

#### 1. 修改 Program.cs

**删除** 第 76-95 行的初始化代码，改为：

```csharp
// 初始化数据库 (使用 FreeSql 迁移管理器，不删除数据)
using (var scope = Host.Services.CreateScope())
{
    try
    {
        var migrationManager = scope.ServiceProvider.GetRequiredService<FreeSqlMigrationManager>();
        migrationManager.InitializeDatabase();

        var stats = migrationManager.GetStatistics();
        Console.WriteLine($"Database initialized: {stats.TableCount} tables, Size: {stats.GetFormattedSize()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}
```

#### 2. 修改 Program.CLI.cs

**替换** 第 52-57 行为：

```csharp
// 初始化数据库 (使用 FreeSql 迁移管理器，不删除数据)
using (var scope = host.Services.CreateScope())
{
    try
    {
        var migrationManager = scope.ServiceProvider.GetRequiredService<FreeSqlMigrationManager>();
        migrationManager.InitializeDatabase();

        var stats = migrationManager.GetStatistics();
        Console.WriteLine($"Database initialized: {stats.TableCount} tables, Size: {stats.GetFormattedSize()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}
```

#### 3. 保持 App.xaml.cs 不变

已经是正确的实现。

---

## 📊 修复前后对比

| 方面 | 修复前 | 修复后 |
|------|--------|--------|
| 初始化位置 | 3 处 | 3 处（统一方式） |
| 数据丢失风险 | ❌ 高（Program.cs 删除数据库） | ✅ 无 |
| 表结构同步 | ⚠️ 部分 | ✅ 完整 |
| 错误处理 | ⚠️ 部分 | ✅ 完整 |
| 初始化统计 | ⚠️ 部分 | ✅ 完整 |
| 代码重复 | ❌ 是 | ✅ 否 |

---

## 🎯 优先级

1. **立即修复** - Program.cs 中的 `EnsureDeleted()` 会导致数据丢失
2. **改进** - Program.CLI.cs 使用统一的初始化方式
3. **保持** - App.xaml.cs 已经是最佳实践

---

## 📝 相关文件

- `Program.cs` - WPF 主程序入口
- `Program.CLI.cs` - CLI 程序入口
- `App.xaml.cs` - WPF 应用启动
- `FreeSqlMigrationManager.cs` - 数据库迁移管理器

---

**建议**: 立即修复 Program.cs，避免数据丢失风险。
