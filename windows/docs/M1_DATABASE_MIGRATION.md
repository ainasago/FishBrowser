# 📊 M1 数据库迁移指南

## 概述

M1 阶段添加了3个新表和2个扩展表。本指南说明如何执行数据库迁移。

## 新增表结构

### 1. ValidationRule (校验规则表)

```sql
CREATE TABLE ValidationRule (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    RuleType TEXT NOT NULL,  -- consistency | realism | cloudflare_risk
    Priority INTEGER NOT NULL DEFAULT 5,  -- 1-10
    Weight INTEGER NOT NULL DEFAULT 100,  -- 0-100
    IsEnabled INTEGER NOT NULL DEFAULT 1,  -- boolean
    ConfigJson TEXT,
    BrowserGroupId INTEGER,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (BrowserGroupId) REFERENCES BrowserGroup(Id) ON DELETE SET NULL,
    INDEX idx_RuleType (RuleType)
);
```

### 2. FingerprintValidationReport (校验报告表)

```sql
CREATE TABLE FingerprintValidationReport (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FingerprintProfileId INTEGER NOT NULL,
    TotalScore INTEGER NOT NULL DEFAULT 0,  -- 0-100
    ConsistencyScore INTEGER NOT NULL DEFAULT 0,
    RealisticScore INTEGER NOT NULL DEFAULT 0,
    CloudflareRiskScore INTEGER NOT NULL DEFAULT 0,
    ScoringFormula TEXT,
    RiskLevel TEXT NOT NULL DEFAULT 'medium',  -- safe | low | medium | high | critical
    CheckResultsJson TEXT,
    RecommendationsJson TEXT,
    Details TEXT,
    ValidatedAt TEXT NOT NULL,
    ValidationVersion TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (FingerprintProfileId) REFERENCES FingerprintProfile(Id) ON DELETE CASCADE,
    INDEX idx_FingerprintProfileId (FingerprintProfileId),
    INDEX idx_ValidatedAt (ValidatedAt),
    INDEX idx_RiskLevel (RiskLevel)
);
```

## 扩展表字段

### BrowserGroup 表新增字段

```sql
ALTER TABLE BrowserGroup ADD COLUMN Icon TEXT DEFAULT '🌐';
ALTER TABLE BrowserGroup ADD COLUMN DefaultProxyId TEXT;
ALTER TABLE BrowserGroup ADD COLUMN DefaultLocale TEXT;
ALTER TABLE BrowserGroup ADD COLUMN DefaultTimezone TEXT;
ALTER TABLE BrowserGroup ADD COLUMN MinRealisticScore INTEGER DEFAULT 70;
ALTER TABLE BrowserGroup ADD COLUMN MaxCloudflareRiskScore INTEGER DEFAULT 50;
```

### FingerprintProfile 表新增字段

```sql
ALTER TABLE FingerprintProfile ADD COLUMN GroupId INTEGER;
ALTER TABLE FingerprintProfile ADD COLUMN RealisticScore INTEGER DEFAULT 0;
ALTER TABLE FingerprintProfile ADD COLUMN LastValidatedAt TEXT;
ALTER TABLE FingerprintProfile ADD COLUMN LastValidationReportId INTEGER;

-- 添加外键约束
ALTER TABLE FingerprintProfile ADD CONSTRAINT fk_GroupId 
    FOREIGN KEY (GroupId) REFERENCES BrowserGroup(Id) ON DELETE SET NULL;
ALTER TABLE FingerprintProfile ADD CONSTRAINT fk_LastValidationReportId 
    FOREIGN KEY (LastValidationReportId) REFERENCES FingerprintValidationReport(Id) ON DELETE SET NULL;
```

## 迁移方式

### 方式 1: 自动迁移 (推荐用于开发)

**步骤**:
1. 关闭应用
2. 删除旧数据库文件 (`webscraper.db`)
3. 重新启动应用
4. 应用会自动创建新表结构

**优点**:
- 自动化，无需手动操作
- 确保表结构完全正确
- 适合开发环境

**缺点**:
- 会丢失现有数据

### 方式 2: 手动迁移 (用于生产环境)

**步骤**:
1. 备份现有数据库
   ```bash
   cp webscraper.db webscraper.db.backup
   ```

2. 使用 SQLite 工具执行迁移脚本
   ```bash
   sqlite3 webscraper.db < migration.sql
   ```

3. 验证迁移
   ```bash
   sqlite3 webscraper.db ".schema ValidationRule"
   sqlite3 webscraper.db ".schema FingerprintValidationReport"
   ```

### 方式 3: EF Core 迁移 (如果启用)

如果项目启用了 EF Core 迁移：

```bash
# 创建迁移
dotnet ef migrations add AddValidationRulesAndReports

# 应用迁移
dotnet ef database update
```

## 验证迁移

### 检查新表是否创建

```bash
sqlite3 webscraper.db ".tables"
```

应该看到:
```
ValidationRule
FingerprintValidationReport
```

### 检查表结构

```bash
sqlite3 webscraper.db ".schema ValidationRule"
sqlite3 webscraper.db ".schema FingerprintValidationReport"
```

### 检查扩展字段

```bash
sqlite3 webscraper.db "PRAGMA table_info(BrowserGroup);"
sqlite3 webscraper.db "PRAGMA table_info(FingerprintProfile);"
```

应该看到新增的字段。

## 数据迁移脚本

### 初始化默认数据

```sql
-- 创建默认分组
INSERT INTO BrowserGroup (Name, Description, Icon, MinRealisticScore, MaxCloudflareRiskScore, CreatedAt)
VALUES 
    ('通用', '通用浏览器分组', '🌐', 70, 50, datetime('now')),
    ('电商爬虫', '用于电商网站爬虫', '🛍️', 75, 40, datetime('now')),
    ('社交媒体', '用于社交媒体爬虫', '📱', 80, 30, datetime('now')),
    ('搜索引擎', '用于搜索引擎爬虫', '🔍', 85, 25, datetime('now'));

-- 创建默认校验规则
INSERT INTO ValidationRule (Name, Description, RuleType, Priority, Weight, IsEnabled, CreatedAt)
VALUES
    ('UA与Platform一致性', '检查User-Agent与Platform是否一致', 'consistency', 10, 100, 1, datetime('now')),
    ('Chrome版本检查', '检查Chrome版本是否为141+', 'realism', 9, 100, 1, datetime('now')),
    ('硬件配置检查', '检查硬件配置是否合理', 'realism', 8, 80, 1, datetime('now')),
    ('防检测数据完整性', '检查防检测数据是否完整', 'cloudflare_risk', 10, 100, 1, datetime('now'));
```

## 回滚方案

如果迁移出现问题，可以回滚：

### 方案 1: 恢复备份

```bash
# 恢复备份
cp webscraper.db.backup webscraper.db

# 重启应用
```

### 方案 2: 删除新表

```bash
sqlite3 webscraper.db "DROP TABLE IF EXISTS ValidationRule;"
sqlite3 webscraper.db "DROP TABLE IF EXISTS FingerprintValidationReport;"
```

然后删除扩展字段（SQLite 不支持 DROP COLUMN，需要重建表）。

## 常见问题

### Q: 迁移后应用无法启动？
**A**: 
1. 检查数据库文件是否损坏
2. 尝试删除数据库文件，让应用重新创建
3. 查看应用日志获取详细错误信息

### Q: 新表没有创建？
**A**:
1. 确保应用已正确启动
2. 检查 FreeSqlMigrationManager 是否被调用
3. 查看日志中的 "Database initialized" 信息

### Q: 扩展字段没有添加？
**A**:
1. SQLite 不支持 ALTER TABLE ADD COLUMN 的某些操作
2. 尝试删除数据库文件重新创建
3. 或使用 EF Core 迁移

### Q: 如何验证迁移成功？
**A**:
```bash
# 查看所有表
sqlite3 webscraper.db ".tables"

# 查看 ValidationRule 表结构
sqlite3 webscraper.db ".schema ValidationRule"

# 查询表中的数据
sqlite3 webscraper.db "SELECT COUNT(*) FROM ValidationRule;"
```

## 性能考虑

### 索引
新增了以下索引以提高查询性能：
- `ValidationRule.RuleType` - 用于按规则类型查询
- `FingerprintValidationReport.FingerprintProfileId` - 用于按指纹查询报告
- `FingerprintValidationReport.ValidatedAt` - 用于按时间查询
- `FingerprintValidationReport.RiskLevel` - 用于按风险等级查询

### 级联删除
- 删除 BrowserGroup 时，关联的 ValidationRule 会被设置为 NULL
- 删除 FingerprintProfile 时，关联的 FingerprintValidationReport 会被级联删除

## 迁移检查清单

- [ ] 备份现有数据库
- [ ] 关闭应用
- [ ] 执行迁移脚本或删除数据库文件
- [ ] 重启应用
- [ ] 验证新表是否创建
- [ ] 验证扩展字段是否添加
- [ ] 检查应用日志是否有错误
- [ ] 测试 BrowserGroupService 功能
- [ ] 测试 FingerprintValidationService 功能
- [ ] 恢复备份（如果需要）

## 相关文档

- [M1_DATA_MODEL_COMPLETE.md](M1_DATA_MODEL_COMPLETE.md) - M1 完成总结
- [QUICK_START_ADVANCED_BROWSER.md](QUICK_START_ADVANCED_BROWSER.md) - 快速启动指南

---

**最后更新**: 2025-11-02
**版本**: 1.0
