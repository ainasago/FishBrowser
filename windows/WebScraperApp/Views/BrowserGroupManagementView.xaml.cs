using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views;

public partial class BrowserGroupManagementView : UserControl
{
    private readonly BrowserGroupService _groupService;
    private readonly ILogService _logService;
    private ObservableCollection<BrowserGroup> _groups;

    public BrowserGroupManagementView()
    {
        InitializeComponent();
        
        // 从 App.xaml.cs 的 Host 获取服务
        if (App.Host?.Services.GetService(typeof(BrowserGroupService)) is BrowserGroupService groupService)
            _groupService = groupService;
        
        if (App.Host?.Services.GetService(typeof(ILogService)) is ILogService logService)
            _logService = logService;

        _groups = new ObservableCollection<BrowserGroup>();
        GroupsListBox.ItemsSource = _groups;

        Loaded += async (s, e) => await LoadGroupsAsync();
    }

    private async void CreateGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateGroupDialog();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var group = await _groupService.CreateGroupAsync(
                    dialog.GroupName,
                    dialog.GroupDescription,
                    dialog.GroupIcon
                );
                
                _groups.Add(group);
                _logService?.LogInfo("BrowserGroupManagementView", $"Created group: {group.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadGroupsAsync();
    }

    private async void GroupsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupsListBox.SelectedItem is BrowserGroup group)
        {
            await DisplayGroupDetailsAsync(group);
        }
    }

    private async void EditGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsListBox.SelectedItem is not BrowserGroup group)
        {
            MessageBox.Show("请先选择一个分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new EditGroupDialog(group);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var updated = await _groupService.UpdateGroupAsync(
                    group.Id,
                    dialog.GroupName,
                    dialog.GroupDescription,
                    dialog.GroupIcon,
                    dialog.MinRealisticScore,
                    dialog.MaxCloudflareRiskScore
                );

                // 更新列表
                var index = _groups.IndexOf(group);
                if (index >= 0)
                {
                    _groups[index] = updated;
                    GroupsListBox.SelectedItem = updated;
                }

                _logService?.LogInfo("BrowserGroupManagementView", $"Updated group: {group.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsListBox.SelectedItem is not BrowserGroup group)
        {
            MessageBox.Show("请先选择一个分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除分组 \"{group.Name}\" 吗？\n\n此操作不可撤销。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _groupService.DeleteGroupAsync(group.Id);
                _groups.Remove(group);
                ClearDetails();
                _logService?.LogInfo("BrowserGroupManagementView", $"Deleted group: {group.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsListBox.SelectedItem is not BrowserGroup group)
        {
            MessageBox.Show("请先选择一个分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(
            $"分组 \"{group.Name}\" 的校验规则:\n\n" +
            $"最小真实性评分: {group.MinRealisticScore}\n" +
            $"最大 Cloudflare 风险评分: {group.MaxCloudflareRiskScore}",
            "校验规则",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _groupService.GetAllGroupsAsync();
            _groups.Clear();
            foreach (var group in groups)
            {
                _groups.Add(group);
            }

            _logService?.LogInfo("BrowserGroupManagementView", $"Loaded {groups.Count} groups");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService?.LogError("BrowserGroupManagementView", $"Failed to load groups: {ex.Message}");
        }
    }

    private async Task DisplayGroupDetailsAsync(BrowserGroup group)
    {
        try
        {
            GroupNameText.Text = group.Name;
            GroupCreatedText.Text = group.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            MinRealisticScoreText.Text = $"{group.MinRealisticScore}/100";
            MaxCloudflareRiskText.Text = $"{group.MaxCloudflareRiskScore}/100";
            EnvironmentCountText.Text = $"({group.Environments.Count})";

            EnvironmentsGrid.ItemsSource = group.Environments;
        }
        catch (Exception ex)
        {
            _logService?.LogError("BrowserGroupManagementView", $"Failed to display group details: {ex.Message}");
        }
    }

    private void ClearDetails()
    {
        GroupNameText.Text = "-";
        GroupCreatedText.Text = "-";
        MinRealisticScoreText.Text = "-";
        MaxCloudflareRiskText.Text = "-";
        EnvironmentCountText.Text = "(0)";
        EnvironmentsGrid.ItemsSource = null;
    }
}
