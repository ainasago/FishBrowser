using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Infrastructure.Data;
using FishBrowser.WPF.Infrastructure.Configuration;
using FishBrowser.WPF.Application.Services;
using FishBrowser.WPF.Presentation.ViewModels;

namespace FishBrowser.WPF;

public partial class App : System.Windows.Application
{
    public static IHost? Host { get; set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 自动安装 Playwright 浏览器
            await PlaywrightInstaller.EnsurePlaywrightInstalledAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to install Playwright: {ex.Message}\n\n" +
                "Please install manually by running:\n" +
                "playwright install",
                "Playwright Installation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        // 构建主机
        Host = new HostBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 使用扩展方法一键配置所有服务
                services.AddAllServices(context.Configuration);
            })
            .Build();

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

                // 字体种子导入（App.xaml 启动路径下也执行一次导入）
                try
                {
                    var fontSvc = scope.ServiceProvider.GetRequiredService<FontService>();
                    await fontSvc.ImportSeedAsync();
                    Console.WriteLine("Font seed import completed (App.xaml startup path)");
                }
                catch (Exception fx)
                {
                    Console.WriteLine($"Font seed import error (App.xaml path): {fx.Message}");
                }

                // GPU 种子导入
                try
                {
                    var gpuSvc = scope.ServiceProvider.GetRequiredService<GpuCatalogService>();
                    await gpuSvc.ImportSeedAsync();
                    Console.WriteLine("GPU seed import completed (App.xaml startup path)");
                }
                catch (Exception gx)
                {
                    Console.WriteLine($"GPU seed import error (App.xaml path): {gx.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 将 Host 存储到资源中供其他组件使用
        this.Resources["Host"] = Host;

        // 记录应用启动日志
        try
        {
            var logSvc = Host.Services.GetRequiredService<LogService>();
            logSvc.LogInfo("App", "Application started successfully");
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"Failed to log startup: {logEx.Message}");
        }

        // 手动创建并显示主窗口，确保此时 Host 已可用
        var mainWindow = new MainWindow();
        this.MainWindow = mainWindow;
        mainWindow.Show();
    }
}
