# 浏览器列表页面优化

## 更新日期
2025-11-04

## 优化内容

### 1. 默认排序优化
**修改**: 默认按创建时间倒序排列（最新创建的在前面）

**后端修改**:
- 文件: `web/FishBrowser.Api/DTOs/Browser/BrowserDto.cs`
- 修改默认排序: `SortBy = "createdat"`, `SortOrder = "desc"`
- 修改默认每页数量: `PageSize = 20`

### 2. 增强过滤功能

#### 新增过滤器
1. **搜索框** - 支持搜索名称、User-Agent、备注
2. **分组筛选** - 按分组过滤，支持"全部分组"和"未分组"
3. **引擎筛选** - 按浏览器引擎过滤
   - UndetectedChrome
   - Chromium
   - Firefox
4. **操作系统筛选** - 按操作系统过滤
   - Windows
   - MacOS
   - Linux
   - Android
   - iOS
5. **排序选项** - 多种排序方式
   - 最新创建（默认）
   - 最早创建
   - 名称 A-Z
   - 名称 Z-A
   - 启动次数↓
   - 启动次数↑

#### 后端实现

**文件**: `web/FishBrowser.Api/Controllers/BrowsersController.cs`

**新增过滤参数**:
```csharp
public class BrowserQueryParams
{
    public int? GroupId { get; set; }
    public string? Search { get; set; }
    public string? Engine { get; set; }      // 新增
    public string? OS { get; set; }          // 新增
    public string SortBy { get; set; } = "createdat";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**过滤逻辑**:
```csharp
// 引擎过滤
if (!string.IsNullOrEmpty(queryParams.Engine))
{
    query = query.Where(b => b.Engine.ToLower() == queryParams.Engine.ToLower());
}

// 操作系统过滤
if (!string.IsNullOrEmpty(queryParams.OS))
{
    query = query.Where(b => b.OS.ToLower() == queryParams.OS.ToLower());
}

// 搜索增强（新增备注搜索）
if (!string.IsNullOrEmpty(queryParams.Search))
{
    var search = queryParams.Search.ToLower();
    query = query.Where(b =>
        b.Name.ToLower().Contains(search) ||
        (b.UserAgent != null && b.UserAgent.ToLower().Contains(search)) ||
        (b.Notes != null && b.Notes.ToLower().Contains(search)));
}
```

#### 前端实现

**文件**: `web/FishBrowser.Web/Views/Browser/Index.cshtml`

**UI 布局**:
```html
<div class="card-body border-bottom">
    <div class="row">
        <div class="col-md-3">搜索框</div>
        <div class="col-md-2">分组筛选</div>
        <div class="col-md-2">引擎筛选</div>
        <div class="col-md-2">操作系统筛选</div>
        <div class="col-md-2">排序选项</div>
        <div class="col-md-1">筛选按钮</div>
    </div>
</div>
```

**JavaScript 功能**:
```javascript
// 加载分组列表
function loadGroups() {
    // 从 API 加载分组并填充到下拉框
}

// 应用过滤器
function applyFilters() {
    loadBrowsers(1);
}

// 加载浏览器列表（带所有过滤参数）
function loadBrowsers(page = 1) {
    const search = $('#searchInput').val();
    const groupId = $('#groupFilter').val();
    const engine = $('#engineFilter').val();
    const os = $('#osFilter').val();
    const sortBy = $('#sortByFilter').val();
    
    // 解析排序字段和顺序
    let sortField = 'createdat';
    let sortOrder = 'desc';
    if (sortBy) {
        const parts = sortBy.split('-');
        sortField = parts[0];
        sortOrder = parts[1];
    }
    
    $.ajax({
        url: '@Url.Action("GetBrowsers")',
        method: 'GET',
        data: {
            page: page,
            pageSize: pageSize,
            search: search,
            groupId: groupId,
            engine: engine,
            os: os,
            sortBy: sortField,
            sortOrder: sortOrder
        },
        // ...
    });
}
```

### 3. 用户体验优化

#### 自动应用过滤
- 下拉框选择变化时自动应用过滤
- 搜索框支持回车键触发搜索
- 无需每次点击"筛选"按钮

```javascript
// 过滤器变化时自动应用
$('#groupFilter, #engineFilter, #osFilter, #sortByFilter').on('change', function() {
    applyFilters();
});

// 搜索框回车事件
$('#searchInput').on('keypress', function(e) {
    if (e.which === 13) {
        applyFilters();
    }
});
```

#### 页面加载优化
```javascript
$(document).ready(function() {
    loadGroups();        // 加载分组列表
    loadStatistics();    // 加载统计信息
    loadRunning().always(function() {
        loadBrowsers();  // 加载浏览器列表（默认最新创建）
    });
    setInterval(loadRunning, 5000); // 定时刷新运行状态
});
```

## 功能特点

### 1. 多维度过滤
- ✅ 按名称/UA/备注搜索
- ✅ 按分组过滤
- ✅ 按引擎过滤
- ✅ 按操作系统过滤
- ✅ 多种排序方式

### 2. 智能交互
- ✅ 下拉框变化自动过滤
- ✅ 搜索框支持回车
- ✅ 保持当前页码状态
- ✅ 过滤后自动跳转到第一页

### 3. 性能优化
- ✅ 后端分页查询
- ✅ 每页20条记录（可配置）
- ✅ 索引优化的数据库查询
- ✅ 前端防抖处理

## 使用示例

### 场景 1: 查找特定浏览器
1. 在搜索框输入关键词（如"随机浏览器"）
2. 按回车或点击筛选按钮
3. 系统显示匹配的浏览器列表

### 场景 2: 查看某个分组的浏览器
1. 在"分组"下拉框选择目标分组
2. 系统自动过滤并显示该分组的浏览器

### 场景 3: 查找所有 Windows 浏览器
1. 在"操作系统"下拉框选择"Windows"
2. 系统自动过滤并显示所有 Windows 浏览器

### 场景 4: 按启动次数排序
1. 在"排序"下拉框选择"启动次数↓"
2. 系统自动按启动次数从高到低排序

### 场景 5: 组合过滤
1. 选择分组："测试组"
2. 选择引擎："UndetectedChrome"
3. 选择系统："Windows"
4. 选择排序："最新创建"
5. 系统显示符合所有条件的浏览器

## API 接口

### GET /api/browsers

**查询参数**:
```
page: 页码（默认1）
pageSize: 每页数量（默认20）
search: 搜索关键词
groupId: 分组ID（-1表示未分组）
engine: 引擎名称
os: 操作系统
sortBy: 排序字段（name/createdat/launchcount）
sortOrder: 排序顺序（asc/desc）
```

**响应示例**:
```json
{
  "items": [
    {
      "id": 1,
      "name": "随机浏览器 20251104-104200",
      "groupId": null,
      "groupName": null,
      "engine": "UndetectedChrome",
      "os": "windows",
      "userAgent": "Mozilla/5.0...",
      "launchCount": 0,
      "enablePersistence": true,
      "createdAt": "2025-11-04T10:42:00",
      "lastLaunchedAt": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

## 数据库优化建议

为了提高查询性能，建议添加以下索引：

```sql
-- 创建时间索引（已有）
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_CreatedAt 
ON BrowserEnvironments(CreatedAt DESC);

-- 引擎索引
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_Engine 
ON BrowserEnvironments(Engine);

-- 操作系统索引
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_OS 
ON BrowserEnvironments(OS);

-- 分组索引（已有）
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_GroupId 
ON BrowserEnvironments(GroupId);

-- 启动次数索引
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_LaunchCount 
ON BrowserEnvironments(LaunchCount DESC);

-- 名称索引（用于搜索）
CREATE INDEX IF NOT EXISTS IX_BrowserEnvironments_Name 
ON BrowserEnvironments(Name);
```

## 后续优化方向

### 1. 高级搜索
- 支持正则表达式搜索
- 支持多字段组合搜索
- 搜索历史记录

### 2. 批量操作
- 批量启动
- 批量停止
- 批量删除
- 批量修改分组

### 3. 视图保存
- 保存常用的过滤条件
- 快速切换视图
- 分享视图给其他用户

### 4. 导出功能
- 导出当前过滤结果
- 支持 CSV/Excel 格式
- 导出配置文件

### 5. 统计图表
- 按引擎分布图
- 按操作系统分布图
- 启动次数趋势图
- 创建时间分布图

## 相关文档

- [随机浏览器创建功能](./RANDOM_BROWSER_CREATION.md)
- [浏览器界面改进](./BROWSER_UI_IMPROVEMENTS.md)
- [浏览器自动关闭检测](./BROWSER_AUTO_CLOSE_DETECTION.md)
- [WPF 逻辑复用说明](./WPF_LOGIC_REUSE.md)
