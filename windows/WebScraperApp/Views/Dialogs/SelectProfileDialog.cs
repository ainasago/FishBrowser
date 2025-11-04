using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Views.Dialogs;

public class SelectProfileDialog : Window
{
    private ListBox _profileList;
    public int? SelectedProfileId { get; private set; }

    public SelectProfileDialog(List<FingerprintProfile> profiles, int? currentProfileId)
    {
        Title = "选择指纹配置";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock { Text = "选择指纹配置：", FontWeight = System.Windows.FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);

        _profileList = new ListBox { Margin = new Thickness(0, 0, 0, 8) };
        var items = new System.Collections.ObjectModel.ObservableCollection<ProfileItem>();
        foreach (var p in profiles)
        {
            items.Add(new ProfileItem
            {
                Id = p.Id,
                Name = p.Name,
                UA = p.UserAgent,
                Locale = p.Locale,
                UpdatedAt = p.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "未知"
            });
            if (p.Id == currentProfileId)
            {
                _profileList.SelectedIndex = items.Count - 1;
            }
        }
        _profileList.ItemsSource = items;
        _profileList.ItemTemplate = CreateItemTemplate();
        Grid.SetRow(_profileList, 1);
        grid.Children.Add(_profileList);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetRow(buttonPanel, 2);

        var okBtn = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        okBtn.Click += (s, e) =>
        {
            if (_profileList.SelectedItem is ProfileItem item)
            {
                SelectedProfileId = item.Id;
                DialogResult = true;
            }
            Close();
        };
        buttonPanel.Children.Add(okBtn);

        var cancelBtn = new Button { Content = "取消", Width = 80 };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        buttonPanel.Children.Add(cancelBtn);

        grid.Children.Add(buttonPanel);
        Content = grid;
    }

    private DataTemplate CreateItemTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        factory.SetValue(StackPanel.MarginProperty, new Thickness(0, 4, 0, 4));

        var nameBlock = new FrameworkElementFactory(typeof(TextBlock));
        nameBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
        nameBlock.SetValue(TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold);
        factory.AppendChild(nameBlock);

        var infoBlock = new FrameworkElementFactory(typeof(TextBlock));
        infoBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("InfoText"));
        infoBlock.SetValue(TextBlock.FontSizeProperty, 11.0);
        infoBlock.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Gray);
        factory.AppendChild(infoBlock);

        return new DataTemplate { VisualTree = factory };
    }

    private class ProfileItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? UA { get; set; }
        public string? Locale { get; set; }
        public string? UpdatedAt { get; set; }
        public string InfoText => $"UA: {UA} | Locale: {Locale} | Updated: {UpdatedAt}";
    }
}
