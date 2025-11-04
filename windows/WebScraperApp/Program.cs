using System;
using System.Threading.Tasks;

namespace FishBrowser.WPF;

public static class Program
{
    [System.STAThread]
    public static async Task Main(string[] args)
    {
        // 仅处理 CLI 模式；GUI 交给 App.xaml.cs 的 OnStartup 处理
        if (args.Length > 0 && args[0] == "--cli")
        {
            var exitCode = await CLIMode.RunAsync(args);
            Environment.Exit(exitCode);
            return;
        }
        // 非 CLI：交由 App.xaml.cs 的 OnStartup 创建和显示主窗体
        var app = new App();
        app.Run();
    }
}
