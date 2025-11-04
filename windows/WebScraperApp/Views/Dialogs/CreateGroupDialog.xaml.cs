using System.Windows;
using System.Windows.Controls;

namespace FishBrowser.WPF.Views;

public partial class CreateGroupDialog : Window
{
    public string GroupName { get; private set; } = string.Empty;
    public string GroupDescription { get; private set; } = string.Empty;
    public string GroupIcon { get; private set; } = "🌐";

    public CreateGroupDialog()
    {
        InitializeComponent();
        SelectIcon("🌐");
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
        SelectedIconText.Text = $"已选择: {icon}";
        
        // 更新按钮样式
        foreach (var child in ((StackPanel)((StackPanel)FindName("IconButton1")).Parent).Children)
        {
            if (child is Button btn)
            {
                btn.Background = btn.Tag?.ToString() == icon 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                
                btn.Foreground = btn.Tag?.ToString() == icon 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            }
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        GroupName = GroupNameInput.Text?.Trim() ?? string.Empty;
        GroupDescription = GroupDescriptionInput.Text?.Trim() ?? string.Empty;

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
