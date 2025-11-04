using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views.Dialogs;

public class GroupEditDialog : Window
{
    private TextBox _nameBox;
    private TextBox _descBox;

    public string GroupName => _nameBox.Text.Trim();
    public string? GroupDescription => string.IsNullOrWhiteSpace(_descBox.Text) ? null : _descBox.Text.Trim();

    public GroupEditDialog(BrowserGroup? existingGroup = null)
    {
        Title = existingGroup == null ? "新建分组" : "编辑分组";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nameLabel = new TextBlock { Text = "名称", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(nameLabel, 0);
        Grid.SetColumn(nameLabel, 0);
        grid.Children.Add(nameLabel);

        _nameBox = new TextBox { Height = 26, Text = existingGroup?.Name ?? string.Empty };
        Grid.SetRow(_nameBox, 0);
        Grid.SetColumn(_nameBox, 1);
        grid.Children.Add(_nameBox);

        var descLabel = new TextBlock { Text = "描述", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetRow(descLabel, 1);
        Grid.SetColumn(descLabel, 0);
        grid.Children.Add(descLabel);

        _descBox = new TextBox { Height = 26, Text = existingGroup?.Description ?? string.Empty, Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetRow(_descBox, 1);
        Grid.SetColumn(_descBox, 1);
        grid.Children.Add(_descBox);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        Grid.SetRow(panel, 2);
        Grid.SetColumnSpan(panel, 2);

        var okBtn = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        okBtn.Click += (s, e) => { DialogResult = !string.IsNullOrWhiteSpace(GroupName); Close(); };
        panel.Children.Add(okBtn);

        var cancelBtn = new Button { Content = "取消", Width = 80 };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        panel.Children.Add(cancelBtn);

        grid.Children.Add(panel);
        Content = grid;
    }
}
