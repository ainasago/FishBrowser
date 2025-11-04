using System;
using System.Threading;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using Microsoft.Playwright;
using LogLevel = FishBrowser.WPF.Models.LogLevel;

namespace FishBrowser.WPF.Services;

/// <summary>
/// DSL 执行器（Phase 1 简化版）
/// </summary>
public class DslExecutor
{
    private readonly ILogService _logger;

    public DslExecutor(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行 DSL 步骤
    /// </summary>
    public async Task ExecuteAsync(
        DslFlow flow,
        IBrowserController controller,
        IProgress<TestProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (flow.Steps == null) return;

        for (int i = 0; i < flow.Steps.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = flow.Steps[i];
            var stepNum = i + 1;

            var stepDesc = GetStepDescription(step);
            progress?.Report(new TestProgress
            {
                Stage = TestStage.ExecutingSteps,
                CurrentStep = stepNum,
                TotalSteps = flow.Steps.Count,
                Message = $"执行步骤 {stepNum}/{flow.Steps.Count}: {stepDesc}",
                Level = LogLevel.Info
            });

            _logger.LogInfo("DslExecutor", $"Step {stepNum}: {stepDesc}");

            try
            {
                // 直接执行步骤（使用控制器提供的方法，兼容 WebView2/Playwright）
                await ExecuteStepDirectAsync(step, controller, cancellationToken);
                
                progress?.Report(new TestProgress
                {
                    Stage = TestStage.ExecutingSteps,
                    CurrentStep = stepNum,
                    TotalSteps = flow.Steps.Count,
                    Message = $"✓ 步骤 {stepNum} 完成",
                    Level = LogLevel.Info
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("DslExecutor", $"Step {stepNum} failed: {ex.Message}", ex.StackTrace);
                throw new Exception($"步骤 {stepNum} 执行失败: {ex.Message}", ex);
            }

            await Task.Delay(300, cancellationToken);
        }
    }

    private string GetStepDescription(DslStep step)
    {
        // 通用 step/Kind 形式优先（兼容 AI 输出的 type: open|fill|click|type）
        var kindKey = step.Kind ?? step.Step;
        if (!string.IsNullOrWhiteSpace(kindKey))
        {
            var kind = kindKey!.ToLowerInvariant();
            return kind switch
            {
                "open" => $"打开 {step.Url}",
                "click" => $"点击元素 {step.Selector?.Type}:{step.Selector?.Value}",
                "type" => $"输入文本 {Truncate(step.Value, 16)} 到 {step.Selector?.Type}:{step.Selector?.Value}",
                "fill" => $"填写表单 {Truncate(step.Value, 16)} 到 {step.Selector?.Type}:{step.Selector?.Value}",
                "waitfor" => $"等待元素 {step.Selector?.Type}:{step.Selector?.Value}",
                _ => $"未知步骤({kind})"
            };
        }

        if (step.Open != null) return $"打开 {step.Open.Url}";
        if (step.Click != null) return $"点击元素 {step.Click.Selector?.Type}:{step.Click.Selector?.Value}";
        if (step.Fill != null) return $"填写表单 到 {step.Fill.Selector?.Type}:{step.Fill.Selector?.Value}";
        if (step.TypeAction != null) return $"输入文本 到 {step.TypeAction.Selector?.Type}:{step.TypeAction.Selector?.Value}";
        if (step.WaitFor != null) return $"等待元素 {step.WaitFor.Selector?.Type}:{step.WaitFor.Selector?.Value}";
        if (step.WaitNetworkIdle != null) return "等待网络空闲";
        if (step.Screenshot != null) return "截图";
        if (step.Log != null) return $"日志: {step.Log.Message}";
        if (step.Sleep != null) return $"等待 {step.Sleep.Ms}ms";
        return "未知步骤";
    }

    private async Task ExecuteStepDirectAsync(
        DslStep step,
        IBrowserController controller,
        CancellationToken cancellationToken)
    {
        // 通用 step/Kind 形式：open/click/type/fill
        var execKindKey = step.Kind ?? step.Step;
        if (!string.IsNullOrWhiteSpace(execKindKey))
        {
            var kind = execKindKey!.ToLowerInvariant();
            switch (kind)
            {
                case "open":
                    if (string.IsNullOrWhiteSpace(step.Url))
                        throw new Exception("open 步骤缺少 url");
                    await controller.NavigateAsync(step.Url);
                    return;
                case "click":
                    var selClick = BuildSelector(step.Selector, controller.ControllerType);
                    await controller.WaitForSelectorAsync(selClick, 30000);
                    await controller.ClickAsync(selClick, 30000);
                    return;
                case "type":
                case "fill":
                    if (step.Value == null) throw new Exception($"{kind} 步骤缺少 value");
                    var sel = BuildSelector(step.Selector, controller.ControllerType);
                    await controller.WaitForSelectorAsync(sel, 30000);
                    await controller.FillAsync(sel, step.Value, 30000);
                    return;
                case "waitfor":
                    var selWait = BuildSelector(step.Selector, controller.ControllerType);
                    await controller.WaitForSelectorAsync(selWait, 30000);
                    return;
                case "waitnetworkidle":
                    await controller.WaitForLoadStateAsync("networkidle", 30000);
                    return;
                case "screenshot":
                    var path = step.Screenshot?.File ?? $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    await controller.ScreenshotAsync(path);
                    _logger.LogInfo("DslExecutor", $"Screenshot saved: {path}");
                    return;
                case "log":
                    var message = step.Log?.Message ?? "";
                    var level = step.Log?.Level?.ToLowerInvariant();
                    if (level == "error")
                        _logger.LogError("DslExecutor", message, null);
                    else if (level == "warn" || level == "warning")
                        _logger.LogWarn("DslExecutor", message);
                    else
                        _logger.LogInfo("DslExecutor", message);
                    return;
                case "sleep":
                    await Task.Delay(step.Sleep?.Ms ?? 1000, cancellationToken);
                    return;
            }
        }

        throw new Exception($"不支持的步骤类型: {execKindKey}");
    }

    // 不再保留 Playwright 专用占位逻辑，所有执行均通过 IBrowserController 实现

    private static string BuildSelector(DslSelector? sel, string controllerType)
    {
        if (sel == null || string.IsNullOrWhiteSpace(sel.Value))
            throw new Exception("缺少 selector");
        var t = (sel.Type ?? "css").ToLowerInvariant();
        var isWebView = string.Equals(controllerType, "WebView2", StringComparison.OrdinalIgnoreCase);
        switch (t)
        {
            case "css":
                return sel.Value;
            case "placeholder":
                return $"input[placeholder=\"{sel.Value}\"]";
            case "text":
            case "role":
            case "xpath":
                if (isWebView)
                    throw new Exception($"选择器类型 {t} 在 WebView2 模式下不支持，请改用 css/placeholder");
                // 在 Playwright 下，直接返回 engine 前缀选择器
                return t == "xpath" ? $"xpath={sel.Value}" : $"{t}={sel.Value}";
            default:
                return sel.Value;
        }
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }
}
