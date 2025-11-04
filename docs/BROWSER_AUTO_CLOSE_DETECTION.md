# 浏览器自动关闭检测功能

## 问题描述

当用户通过Web界面启动浏览器后，在浏览器窗口中直接关闭浏览器时，Web页面无法自动识别浏览器已被关闭，导致状态显示不准确。

## 解决方案

### 核心机制

实现了基于 `WaitForCloseAsync()` 的浏览器关闭监控机制：

1. **启动时监控**：当浏览器通过 `BrowserControllerAdapter` 启动时，同时启动一个后台任务来监控浏览器关闭事件
2. **异步等待**：后台任务调用 `controller.WaitForCloseAsync()` 等待浏览器关闭
3. **自动清理**：浏览器关闭后，自动从运行中列表移除并释放资源

### 代码修改

#### 1. BrowserLaunchService.cs

**修改的关键部分：**

```csharp
// 改用 Dictionary 存储 BrowserControllerAdapter 实例
private readonly Dictionary<int, BrowserControllerAdapter> _runningBrowserControllers = new();

// 启动浏览器时保存控制器实例并启动监控
var controller = await LaunchUsingBrowserControllerAsync(browser, profile);
_runningBrowserControllers[browserId] = controller;

// 启动后台任务监控浏览器关闭
_ = Task.Run(async () => await MonitorBrowserCloseAsync(browserId, controller));
```

**新增监控方法：**

```csharp
/// <summary>
/// 监控浏览器关闭事件
/// </summary>
private async Task MonitorBrowserCloseAsync(int browserId, BrowserControllerAdapter controller)
{
    try
    {
        _logger.LogInformation("Started monitoring browser {Id} for closure", browserId);
        
        // 等待浏览器关闭
        await controller.WaitForCloseAsync();
        
        // 浏览器已关闭，从追踪器中移除
        _runningBrowserControllers.Remove(browserId);
        
        _logger.LogInformation("Browser {Id} has been closed by user", browserId);
        
        // 释放控制器资源
        await controller.DisposeAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error monitoring browser {Id} closure", browserId);
        // 发生错误时也要清理
        _runningBrowserControllers.Remove(browserId);
    }
}
```

**更新 StopBrowser 为异步方法：**

```csharp
public async Task<bool> StopBrowserAsync(int browserId)
{
    try
    {
        // 检查是否有 BrowserControllerAdapter 实例
        if (_runningBrowserControllers.TryGetValue(browserId, out var controller))
        {
            _runningBrowserControllers.Remove(browserId);
            await controller.DisposeAsync();
            _logger.LogInformation("Browser {Id} stopped via controller", browserId);
            return true;
        }
        
        // 检查是否有 Process 实例（向后兼容）
        if (_runningBrowsers.TryGetValue(browserId, out var process))
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit(5000);
            }
            _runningBrowsers.Remove(browserId);
            _logger.LogInformation("Browser {Id} stopped via process", browserId);
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error stopping browser {Id}", browserId);
        return false;
    }
}
```

#### 2. BrowsersController.cs

更新 StopBrowser 方法为异步：

```csharp
[HttpPost("{id}/stop")]
public async Task<ActionResult> StopBrowser(int id, [FromServices] FishBrowser.Api.Services.BrowserLaunchService launchService)
{
    try
    {
        var success = await launchService.StopBrowserAsync(id);
        if (success)
        {
            return Ok(new { success = true, message = "浏览器已停止" });
        }
        else
        {
            return NotFound(new { success = false, error = "浏览器未在运行" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error stopping browser {Id}", id);
        return BadRequest(new { success = false, error = ex.Message });
    }
}
```

## 工作流程

1. **用户启动浏览器**
   - Web界面调用 `/api/browsers/{id}/launch`
   - `BrowserLaunchService` 创建 `BrowserControllerAdapter` 实例
   - 将实例保存到 `_runningBrowserControllers` 字典
   - 启动后台监控任务

2. **后台监控运行**
   - 监控任务调用 `controller.WaitForCloseAsync()`
   - 该方法会阻塞直到浏览器关闭

3. **用户关闭浏览器**
   - 用户在浏览器窗口点击关闭按钮
   - `WaitForCloseAsync()` 检测到关闭事件并返回
   - 监控任务自动从 `_runningBrowserControllers` 移除该浏览器
   - 调用 `DisposeAsync()` 释放资源

4. **前端自动更新**
   - 前端每5秒调用 `/api/browsers/running` 获取运行中的浏览器列表
   - `GetRunningBrowsers()` 方法返回 `_runningBrowserControllers` 中的浏览器
   - 前端检测到浏览器不在运行列表中，自动更新UI状态

## 优势

1. **自动检测**：无需手动刷新，浏览器关闭后自动更新状态
2. **资源清理**：确保浏览器关闭后正确释放所有资源
3. **实时性**：通过定时轮询（5秒）实现近实时的状态更新
4. **可靠性**：使用异常处理确保即使出错也能正确清理资源

## 前端配置

前端已有的定时刷新机制（Index.cshtml）：

```javascript
// 定时刷新运行中状态（每5秒）
setInterval(loadRunning, 5000);
```

这确保了前端能够及时发现浏览器状态的变化。

## 测试建议

1. 启动一个浏览器
2. 在浏览器窗口中直接关闭浏览器
3. 等待最多5秒，观察Web界面是否自动更新状态
4. 检查日志确认监控任务正常工作

## 日志输出

成功的日志示例：

```
[Information] Started monitoring browser 123 for closure
[Information] Browser 123 has been closed by user
[Information] Browser 123 stopped via controller
```
