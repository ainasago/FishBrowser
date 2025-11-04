# 数据库路径统一配置

## 更新日期
2025-11-04

## 变更说明

将数据库文件统一移动到 API 根目录，并更新所有相关配置。

## 数据库位置

**新位置**: `d:\1Dev\webbrowser\web\FishBrowser.Api\webscraper.db`

**原位置**: `d:\1Dev\webbrowser\windows\WebScraperApp\webscraper.db`

## 配置修改

### 1. API 配置

**文件**: `web/FishBrowser.Api/appsettings.json`

```json
{
  "Database": {
    "Path": "webscraper.db"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=webscraper.db"
  }
}
```

**说明**: 使用相对路径，数据库文件位于 API 项目根目录。

### 2. WPF 配置

**文件**: `windows/WebScraperApp/appsettings.json`

```json
{
  "Database": {
    "ConnectionString": "Data Source=..\\..\\web\\FishBrowser.Api\\webscraper.db"
  }
}
```

**说明**: 使用相对路径，从 WPF 项目目录指向 API 根目录的数据库文件。

## 路径结构

```
d:\1Dev\webbrowser\
├── web\
│   └── FishBrowser.Api\
│       ├── webscraper.db          ← 数据库文件（新位置）
│       └── appsettings.json       ← 使用相对路径
└── windows\
    └── WebScraperApp\
        └── appsettings.json       ← 使用相对路径指向 API 目录
```

## 优势

### 1. 统一数据源
- ✅ Web 端和 WPF 端共享同一个数据库
- ✅ 数据实时同步，无需手动复制
- ✅ 避免数据不一致问题

### 2. 便于部署
- ✅ API 服务独立部署时数据库随项目一起
- ✅ 数据库备份更简单
- ✅ 便于容器化部署

### 3. 开发便利
- ✅ 两端修改数据立即生效
- ✅ 便于调试和测试
- ✅ 减少配置复杂度

## 迁移步骤

如果需要迁移现有数据：

### 方法 1: 复制数据库文件

```bash
# 从 WPF 目录复制到 API 目录
copy "d:\1Dev\webbrowser\windows\WebScraperApp\webscraper.db" "d:\1Dev\webbrowser\web\FishBrowser.Api\webscraper.db"
```

### 方法 2: 使用 SQLite 工具导出导入

1. 使用 DB Browser for SQLite 打开旧数据库
2. 导出数据为 SQL 文件
3. 在新位置创建数据库并导入

### 方法 3: 让 API 自动创建

如果不需要保留旧数据：

1. 删除旧数据库文件
2. 启动 API 服务
3. API 会自动创建新数据库并初始化表结构

## 注意事项

### 1. 文件权限
确保 API 进程有权限读写数据库文件：

```bash
# Windows
icacls "webscraper.db" /grant Users:F
```

### 2. 并发访问
SQLite 支持多进程读取，但写入时会锁定：

- ✅ 多个读取操作可以并发
- ⚠️ 写入操作会短暂锁定数据库
- ⚠️ WPF 和 API 同时写入可能需要重试

### 3. 备份建议

定期备份数据库文件：

```bash
# 创建备份
copy "webscraper.db" "webscraper_backup_20251104.db"

# 或使用 SQLite 命令
sqlite3 webscraper.db ".backup webscraper_backup.db"
```

### 4. 开发环境 vs 生产环境

**开发环境**: 使用相对路径（当前配置）

**生产环境**: 建议使用绝对路径或环境变量

```json
{
  "Database": {
    "Path": "${DATABASE_PATH}/webscraper.db"
  }
}
```

## 故障排查

### 问题 1: 找不到数据库文件

**症状**: `SQLite Error: unable to open database file`

**解决**:
1. 检查数据库文件是否存在
2. 检查路径配置是否正确
3. 检查文件权限

### 问题 2: 数据库被锁定

**症状**: `SQLite Error: database is locked`

**解决**:
1. 关闭所有访问数据库的程序
2. 检查是否有未释放的连接
3. 重启 API 和 WPF 应用

### 问题 3: 数据不同步

**症状**: WPF 看不到 API 创建的数据

**解决**:
1. 确认两端使用同一个数据库文件
2. 检查配置文件路径
3. 刷新 WPF 页面

## 相关文档

- [浏览器列表优化](./BROWSER_LIST_OPTIMIZATION.md)
- [随机浏览器创建功能](./RANDOM_BROWSER_CREATION.md)
