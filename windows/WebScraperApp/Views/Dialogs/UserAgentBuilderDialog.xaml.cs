using System;
using System.Windows;

namespace FishBrowser.WPF.Views
{
    public partial class UserAgentBuilderDialog : Window
    {
        public string BrowserName => (BrowserNameBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Edge";
        public string BrowserVersion => BrowserVersionBox.Text?.Trim() ?? "141.0.0.0";
        public string BrowserMajor => BrowserMajorBox.Text?.Trim() ?? "141";
        public string Engine => (EngineBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Blink";
        public string EngineVersion => EngineVersionBox.Text?.Trim() ?? BrowserVersion;
        public string OS => (OsBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Windows";
        public string OSVersion => OsVersionBox.Text?.Trim() ?? "10";
        public string CpuArch => (CpuArchBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "amd64";
        public string DeviceModel => (DeviceModelBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Windows PC";
        public string AppleWebKitVersion => AppleWebKitVersionBox.Text?.Trim() ?? "537.36";
        public string SafariVersion => SafariVersionBox.Text?.Trim() ?? BrowserMajor;
        public string PlatformVersion => PlatformVersionBox.Text?.Trim() ?? OSVersion;
        public bool GenerateCH => GenerateCHCheck.IsChecked == true;
        public bool IsMobile => IsMobileCheck.IsChecked == true;

        public UserAgentBuilderDialog()
        {
            InitializeComponent();
            // defaults
            BrowserNameBox.SelectedIndex = 0;
            EngineBox.SelectedIndex = 0;
            OsBox.SelectedIndex = 0;
            CpuArchBox.SelectedIndex = 2; // amd64
            DeviceModelBox.SelectedIndex = 7; // Windows PC
            GenerateCHCheck.IsChecked = true;
            IsMobileCheck.IsChecked = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
