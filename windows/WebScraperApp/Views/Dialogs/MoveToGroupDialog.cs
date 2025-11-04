using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views.Dialogs;

public class MoveToGroupDialog : Window
{
    private ComboBox _groupCombo;
    public int? SelectedGroupId { get; private set; }

    public MoveToGroupDialog(List<BrowserGroup> groups, int? currentGroupId)
    {
        Title = "移动到分组";
        Width = 350;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = "目标分组", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        _groupCombo = new ComboBox { Height = 26, DisplayMemberPath = "Name" };
        var items = new System.Collections.ObjectModel.ObservableCollection<object>();
        items.Add(new { Id = (int?)null, Name = "未分组" });
        foreach (var g in groups) items.Add(g);
        _groupCombo.ItemsSource = items;
        _groupCombo.SelectedIndex = 0;
        Grid.SetRow(_groupCombo, 0);
        Grid.SetColumn(_groupCombo, 1);
        grid.Children.Add(_groupCombo);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        Grid.SetRow(panel, 1);
        Grid.SetColumnSpan(panel, 2);

        var okBtn = new Button { Content = "移动", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        okBtn.Click += (s, e) =>
        {
            var item = _groupCombo.SelectedItem;
            if (item is BrowserGroup group) SelectedGroupId = group.Id;
            else SelectedGroupId = null;
            DialogResult = true;
            Close();
        };
        panel.Children.Add(okBtn);

        var cancelBtn = new Button { Content = "取消", Width = 80 };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        panel.Children.Add(cancelBtn);

        grid.Children.Add(panel);
        Content = grid;
    }
}
