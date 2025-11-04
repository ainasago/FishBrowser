# 会话持久化故障排查指南

## 日志检查清单

### 1. 启动阶段日志（应该看到）

```
[INFO] BrowserMgmt: ========== Starting browser launch for env: xxx (ID: 1) ==========
[INFO] BrowserMgmt: Loaded fingerprint profile: xxx (ID: 1)
[INFO] BrowserMgmt: Persistence enabled, initializing session path...
[INFO] BrowserSession: InitializeSessionPath called for env xxx (ID: 1)
[INFO] BrowserSession: Creating new session path: D:\...\BrowserSessions\env_1_xxx
[INFO] BrowserSession: Session directory created successfully
[INFO] BrowserSession: Session path saved to database
[INFO] BrowserMgmt: Session path initialized: D:\...\BrowserSessions\env_1_xxx
[INFO] BrowserMgmt: Creating PlaywrightController...
[INFO] BrowserMgmt: Initializing browser...
[INFO] PlaywrightController: Starting persistent context initialization, userDataPath: D:\...
[INFO] PlaywrightController: Launching persistent context at: D:\...
[INFO] PlaywrightController: Persistent context launched successfully, pages count: 1
[INFO] PlaywrightController: Fingerprint injection script added
[INFO] PlaywrightController: Browser initialized with fingerprint: xxx (mode: persistent, path: ...)
[INFO] BrowserMgmt: Browser initialized successfully
[INFO] BrowserSession: RecordLaunch called for env ID: 1
[INFO] BrowserSession: Launch recorded for env xxx: 0 -> 1, time: 2025-10-28 23:51:00
[INFO] BrowserMgmt: Launch recorded, count: 1
[INFO] BrowserMgmt: Navigating to test page...
[INFO] BrowserMgmt: Navigation completed
[INFO] BrowserMgmt: Starting background task to wait for browser close...
[INFO] BrowserMgmt: Background task started
[INFO] BrowserMgmt: Background task: Calling WaitForCloseAsync...
[INFO] PlaywrightController: Starting to wait for browser context close...
[INFO] PlaywrightController: Close event handler registered, waiting for browser to close...
```

### 2. 关闭阶段日志（应该看到）

```
[INFO] PlaywrightController: Context Close event triggered!
[INFO] PlaywrightController: Browser context closed, session data should be saved now
[INFO] PlaywrightController: Session data save completed
[INFO] BrowserMgmt: ========== Browser 'xxx' closed, session saved ==========
```

### 3. 再次启动日志（应该看到）

```
[INFO] BrowserSession: InitializeSessionPath called for env xxx (ID: 1)
[INFO] BrowserSession: Using existing session path: D:\...\BrowserSessions\env_1_xxx
[INFO] BrowserSession: Session directory contains XXX files  <-- 如果有数据，这个数字应该 > 0
```

## 常见问题诊断

### 问题 1：没有看到 "Context Close event triggered!"

**症状**：
- 关闭浏览器后，日志停在 "waiting for browser to close..."
- 没有看到 "Context Close event triggered!"

**可能原因**：
1. Playwright 的 Close 事件没有正确触发
2. 浏览器进程被强制终止（而不是正常关闭）

**解决方案**：
- 确保点击浏览器窗口的 X 按钮关闭
- 不要使用任务管理器强制结束进程
- 检查是否有其他代码调用了 `DisposeAsync()`

### 问题 2：文件数量为 0

**症状**：
```
[INFO] BrowserSession: Session directory contains 0 files
```

**可能原因**：
- Playwright 没有正确初始化持久化上下文
- userDataPath 路径不正确
- 权限问题

**解决方案**：
1. 检查 userDataPath 是否正确：
   ```
   [INFO] PlaywrightController: Launching persistent context at: [路径]
   ```
2. 手动检查该目录是否存在
3. 检查应用是否有写入权限

### 问题 3：后台任务没有启动

**症状**：
- 没有看到 "Background task started"
- 没有看到 "Background task: Calling WaitForCloseAsync..."

**可能原因**：
- EnablePersistence 为 false
- 代码执行异常

**解决方案**：
1. 检查环境的 EnablePersistence 设置
2. 查看是否有异常日志

### 问题 4：会话数据没有保存

**症状**：
- 看到所有日志都正常
- 但再次启动时标签页没有恢复

**可能原因**：
1. Playwright 的持久化上下文可能不保存标签页
2. 浏览器版本问题
3. 数据写入磁盘延迟

**解决方案**：
1. 关闭浏览器后等待 2-3 秒
2. 检查会话目录中的文件：
   - `Default/Cookies` - Cookie 数据
   - `Default/History` - 历史记录
   - `Default/Preferences` - 浏览器设置
3. 手动验证 Cookie 是否保存：
   - 登录一个网站
   - 关闭浏览器
   - 再次打开
   - 检查是否还在登录状态

## 手动验证步骤

### 步骤 1：检查会话目录

1. 启动浏览器后，从日志中找到 userDataPath
2. 打开文件管理器，导航到该路径
3. 检查目录结构：
   ```
   env_1_xxx/
   ├─ Default/
   │  ├─ Cookies
   │  ├─ History
   │  ├─ Preferences
   │  └─ ...
   └─ ...
   ```

### 步骤 2：验证 Cookie 保存

1. 启动浏览器
2. 访问 https://github.com 并登录
3. 关闭浏览器（等待日志显示 "session saved"）
4. 再次启动浏览器
5. 访问 https://github.com
6. **预期结果**：应该自动登录

### 步骤 3：验证历史记录

1. 启动浏览器
2. 访问几个网站
3. 关闭浏览器
4. 再次启动
5. 按 Ctrl+H 查看历史记录
6. **预期结果**：应该看到之前访问的网站

### 步骤 4：验证扩展

1. 启动浏览器
2. 安装一个扩展（如 uBlock Origin）
3. 关闭浏览器
4. 再次启动
5. **预期结果**：扩展应该自动加载

## Playwright 限制说明

### 标签页恢复

⚠️ **重要**：Playwright 的持久化上下文**不会自动恢复标签页**！

这是 Playwright 的设计限制，不是我们的 bug。持久化上下文只保存：
- ✅ Cookie
- ✅ LocalStorage
- ✅ 历史记录
- ✅ 扩展
- ✅ 书签
- ❌ 打开的标签页（不保存）

### 解决方案

如果需要恢复标签页，有两个选择：

#### 方案 1：使用浏览器自带的恢复功能
在浏览器设置中启用"启动时恢复上次会话"

#### 方案 2：手动保存标签页
我们可以在关闭前保存所有打开的标签页 URL，下次启动时自动打开：

```csharp
// 关闭前保存标签页
var pages = await _context.PagesAsync();
var urls = pages.Select(p => p.Url).ToList();
// 保存到数据库或文件

// 下次启动时恢复
foreach (var url in savedUrls)
{
    await _context.NewPageAsync();
    await page.GotoAsync(url);
}
```

## 日志文件位置

日志应该显示在应用的日志面板中。如果需要查看完整日志：

1. 点击侧边栏"日志"按钮
2. 筛选 "BrowserMgmt", "BrowserSession", "PlaywrightController"
3. 按时间排序查看完整流程

## 需要提供的信息

如果问题仍然存在，请提供：

1. **完整日志**（从启动到关闭）
2. **会话目录路径**
3. **会话目录文件列表**（截图或文件数量）
4. **操作步骤**：
   - 打开了哪些网站
   - 是否登录
   - 如何关闭浏览器
5. **再次启动后的现象**

## 快速测试脚本

如果需要快速验证会话是否保存，可以：

1. 启动浏览器
2. 在地址栏输入：`chrome://version/`
3. 查看 "个人资料路径"，应该指向我们的 userDataPath
4. 访问一个网站并登录
5. 关闭浏览器
6. 手动检查 userDataPath 目录中的 `Default/Cookies` 文件
7. 如果文件存在且大小 > 0，说明 Cookie 已保存
