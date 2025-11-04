using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views;

public partial class EditGroupDialog : Window
{
    private readonly BrowserGroup _group;
    
    public string GroupName { get; private set; } = string.Empty;
    public string GroupDescription { get; private set; } = string.Empty;
    public string GroupIcon { get; private set; } = "🌐";
    public int MinRealisticScore { get; private set; } = 70;
    public int MaxCloudflareRiskScore { get; private set; } = 50;

    public EditGroupDialog(BrowserGroup group)
    {
        InitializeComponent();
        _group = group;
        
        // 初始化值
        GroupNameInput.Text = group.Name;
        GroupDescriptionInput.Text = group.Description ?? string.Empty;
        GroupIcon = group.Icon ?? "🌐";
        MinRealisticScore = group.MinRealisticScore;
        MaxCloudflareRiskScore = group.MaxCloudflareRiskScore;
        
        MinRealisticScoreSlider.Value = MinRealisticScore;
        MaxCloudflareRiskSlider.Value = MaxCloudflareRiskScore;
        
        UpdateSliderLabels();
        SelectIcon(GroupIcon);
        
        MinRealisticScoreSlider.ValueChanged += (s, e) => UpdateSliderLabels();
        MaxCloudflareRiskSlider.ValueChanged += (s, e) => UpdateSliderLabels();
    }

    private void UpdateSliderLabels()
    {
        MinRealisticScoreText.Text = $"当前值: {(int)MinRealisticScoreSlider.Value}";
        MaxCloudflareRiskText.Text = $"当前值: {(int)MaxCloudflareRiskSlider.Value}";
    }

    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string icon)
        {
            SelectIcon(icon);
        }
    }

    private void SelectIcon(string icon)
    {
        GroupIcon = icon;
        
        // 更新按钮样式
        var buttons = new[] { IconButton1, IconButton2, IconButton3, IconButton4, IconButton5 };
        foreach (var btn in buttons)
        {
            if (btn.Tag?.ToString() == icon)
            {
                btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
            else
            {
                btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        GroupName = GroupNameInput.Text?.Trim() ?? string.Empty;
        GroupDescription = GroupDescriptionInput.Text?.Trim() ?? string.Empty;
        MinRealisticScore = (int)MinRealisticScoreSlider.Value;
        MaxCloudflareRiskScore = (int)MaxCloudflareRiskSlider.Value;

        if (string.IsNullOrEmpty(GroupName))
        {
            MessageBox.Show("请输入分组名称", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
