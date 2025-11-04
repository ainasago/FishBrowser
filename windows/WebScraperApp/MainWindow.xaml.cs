using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Tests;
using Microsoft.Win32;
using FishBrowser.WPF.Views;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF;

public partial class MainWindow : Window
{
    private readonly IHost _host;

    public MainWindow()
    {
        this.InitializeComponent();

        // 从 DI 容器获取服务
        _host = App.Current.Resources["Host"] as IHost ?? throw new System.InvalidOperationException("Host not found");
        
        // 启动时直接显示Dashboard页面
        MainFrame.Navigate(new Uri("Views/DashboardView.xaml", UriKind.Relative));
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/DashboardView.xaml", UriKind.Relative));
    }

    private void TaskManagement_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/TaskManagementView.xaml", UriKind.Relative));
    }

    private void StagehandAITask_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new StagehandTaskView());
    }

    private void AITask_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/AITaskView.xaml", UriKind.Relative));
    }

    private void FingerprintConfig_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/FingerprintConfigView.xaml", UriKind.Relative));
    }

    private void ProxyPool_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/ProxyPoolView.xaml", UriKind.Relative));
    }

    private void AIResults_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/AIResultsView.xaml", UriKind.Relative));
    }

    private void Logs_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/LogsView.xaml", UriKind.Relative));
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/SettingsView.xaml", UriKind.Relative));
    }

    private void AIProviderConfig_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/AIProviderManagementView.xaml", UriKind.Relative));
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/HelpView.xaml", UriKind.Relative));
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestButton.IsEnabled = false;
            TestButton.Content = "测试运行中...";

            // Run the sample test asynchronously
            await Task.Run(() => SampleTest.RunSampleAsync(_host).GetAwaiter().GetResult());

            MessageBox.Show("测试完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestButton.IsEnabled = true;
            TestButton.Content = "运行测试";
        }
    }

    // 元数据管理
    private void OpenPresetManagement_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/PresetManagementPage.xaml", UriKind.Relative));
    }

    private void OpenMetaEditor_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/TraitMetaEditorPage.xaml", UriKind.Relative));
    }

    private void OpenTraitCatalog_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/TraitCatalogPage.xaml", UriKind.Relative));
    }

    private void OpenBatchGeneration_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new Uri("Views/BatchGenerationPage.xaml", UriKind.Relative));
    }

    private void OpenBrowserManagement_Click(object sender, RoutedEventArgs e)
    {
        // 使用新的浏览器管理页面（已集成分组功能）
        MainFrame.Navigate(new Uri("Views/BrowserManagementPageV2.xaml", UriKind.Relative));
    }

    private async void ImportCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Catalog JSON (*.json)|*.json|All Files (*.*)|*.*",
                Title = "导入元目录 JSON"
            };
            if (ofd.ShowDialog(this) == true)
            {
                var io = _host.Services.GetRequiredService<MetaCatalogIoService>();
                await io.ImportAsync(ofd.FileName, merge: true);
                MessageBox.Show("导入成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Catalog JSON (*.json)|*.json|All Files (*.*)|*.*",
                Title = "导出元目录 JSON",
                FileName = $"catalog-{DateTime.Now:yyyyMMdd-HHmmss}.json"
            };
            if (sfd.ShowDialog(this) == true)
            {
                var io = _host.Services.GetRequiredService<MetaCatalogIoService>();
                await io.ExportAsync(sfd.FileName);
                MessageBox.Show("导出成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
