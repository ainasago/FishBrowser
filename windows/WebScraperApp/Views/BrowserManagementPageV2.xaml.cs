using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Views.Dialogs;

namespace FishBrowser.WPF.Views
{
    public partial class BrowserManagementPageV2 : Page
    {
        private IHost _host;
        private WebScraperDbContext _db;
        private ILogService _log;
        private BrowserEnvironmentService _svc;
        private BrowserSessionService _sessionSvc;
        
        private ObservableCollection<BrowserEnvironmentViewModel> _allEnvironments;
        private ObservableCollection<BrowserEnvironmentViewModel> _filteredEnvironments;
        private int? _selectedGroupId;
        private bool _isCardView = true;

        public BrowserManagementPageV2()
        {
            InitializeComponent();
            _host = App.Host ?? throw new InvalidOperationException("Host not found");
            _db = _host.Services.GetRequiredService<WebScraperDbContext>();
            _log = _host.Services.GetRequiredService<ILogService>();
            _svc = _host.Services.GetRequiredService<BrowserEnvironmentService>();
            _sessionSvc = _host.Services.GetRequiredService<BrowserSessionService>();

            _allEnvironments = new ObservableCollection<BrowserEnvironmentViewModel>();
            _filteredEnvironments = new ObservableCollection<BrowserEnvironmentViewModel>();

            Loaded += (s, e) => LoadData();
        }

        #region 数据加载

        private void LoadData()
        {
            LoadGroups();
            LoadEnvironments();
            UpdateStatistics();
        }

        private void LoadGroups()
        {
            var groups = _svc.GetAllGroups();
            
            GroupTree.Items.Clear();
            
            // 添加"全部"节点
            var allItem = new TreeViewItem
            {
                Header = "📂 全部浏览器",
                Tag = null,
                IsSelected = true
            };
            GroupTree.Items.Add(allItem);

            // 添加"未分组"节点
            var ungroupedItem = new TreeViewItem
            {
                Header = "📄 未分组",
                Tag = -1
            };
            GroupTree.Items.Add(ungroupedItem);

            // 添加分组节点
            foreach (var group in groups)
            {
                var item = new TreeViewItem
                {
                    Header = $"📁 {group.Name}",
                    Tag = group.Id
                };
                GroupTree.Items.Add(item);
            }

            GroupCountText.Text = groups.Count.ToString();
        }

        private void LoadEnvironments()
        {
            List<BrowserEnvironment> envs;
            
            if (_selectedGroupId == null)
            {
                // 全部
                envs = _svc.GetAllEnvironments();
                CurrentGroupTitle.Text = "全部浏览器";
            }
            else if (_selectedGroupId == -1)
            {
                // 未分组
                envs = _svc.GetEnvironmentsByGroup(null);
                CurrentGroupTitle.Text = "未分组";
            }
            else
            {
                // 指定分组
                envs = _svc.GetEnvironmentsByGroup(_selectedGroupId.Value);
                var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
                CurrentGroupTitle.Text = group?.Name ?? "未知分组";
            }

            _allEnvironments.Clear();
            foreach (var env in envs)
            {
                _allEnvironments.Add(new BrowserEnvironmentViewModel(env));
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var searchText = SearchBox.Text?.ToLower() ?? "";
            
            // 获取过滤条件
            var engineFilter = (EngineFilterComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var osFilter = (OSFilterComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            
            var filtered = _allEnvironments.Where(e =>
            {
                // 搜索过滤
                var matchSearch = string.IsNullOrEmpty(searchText) ||
                    e.Name.ToLower().Contains(searchText) ||
                    (e.FingerprintProfile?.UserAgent?.ToLower().Contains(searchText) ?? false) ||
                    (e.Notes?.ToLower().Contains(searchText) ?? false);
                
                // 引擎过滤
                var matchEngine = string.IsNullOrEmpty(engineFilter) ||
                    (e.Engine?.Equals(engineFilter, StringComparison.OrdinalIgnoreCase) ?? false);
                
                // 操作系统过滤
                var matchOS = string.IsNullOrEmpty(osFilter) ||
                    (e.OS?.Equals(osFilter, StringComparison.OrdinalIgnoreCase) ?? false);
                
                return matchSearch && matchEngine && matchOS;
            }).ToList();

            // 应用排序
            filtered = ApplySorting(filtered);

            _filteredEnvironments.Clear();
            foreach (var env in filtered)
            {
                _filteredEnvironments.Add(env);
            }

            if (_isCardView)
            {
                BrowserCardList.ItemsSource = _filteredEnvironments;
            }
            else
            {
                BrowserListGrid.ItemsSource = _filteredEnvironments;
            }

            FilteredCountText.Text = $"显示 {_filteredEnvironments.Count} 个";
            UpdateStatistics();
        }

        private List<BrowserEnvironmentViewModel> ApplySorting(List<BrowserEnvironmentViewModel> list)
        {
            var sortTag = (SortComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "createdat-desc";
            
            return sortTag switch
            {
                "createdat-desc" => list.OrderByDescending(e => e.CreatedAt).ToList(),
                "createdat-asc" => list.OrderBy(e => e.CreatedAt).ToList(),
                "name-asc" => list.OrderBy(e => e.Name).ToList(),
                "name-desc" => list.OrderByDescending(e => e.Name).ToList(),
                "launchcount-desc" => list.OrderByDescending(e => e.LaunchCount).ToList(),
                "launchcount-asc" => list.OrderBy(e => e.LaunchCount).ToList(),
                _ => list.OrderByDescending(e => e.CreatedAt).ToList()
            };
        }

        private void UpdateStatistics()
        {
            TotalCountText.Text = _allEnvironments.Count.ToString();
            RunningCountText.Text = "0"; // TODO: 实现运行状态检测
            
            UpdateBatchOperationBar();
        }

        private void UpdateBatchOperationBar()
        {
            var selectedCount = _filteredEnvironments.Count(e => e.IsSelected);
            
            if (selectedCount > 0)
            {
                BatchOperationBar.Visibility = Visibility.Visible;
                SelectedCountText.Text = $"已选中 {selectedCount} 个浏览器";
            }
            else
            {
                BatchOperationBar.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region 事件处理

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 控制占位符显示
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            
            ApplyFilter();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void GroupTree_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (GroupTree.SelectedItem is TreeViewItem item)
            {
                var tag = item.Tag;
                if (tag == null)
                {
                    _selectedGroupId = null; // 全部
                }
                else if (tag is int groupId)
                {
                    _selectedGroupId = groupId == -1 ? null : groupId;
                }
                
                LoadEnvironments();
            }
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            // 页面初始化时可能触发，需要检查
            if (_filteredEnvironments == null) return;
            
            _isCardView = CardViewRadio.IsChecked == true;
            
            if (_isCardView)
            {
                CardViewScroll.Visibility = Visibility.Visible;
                BrowserListGrid.Visibility = Visibility.Collapsed;
                BrowserCardList.ItemsSource = _filteredEnvironments;
            }
            else
            {
                CardViewScroll.Visibility = Visibility.Collapsed;
                BrowserListGrid.Visibility = Visibility.Visible;
                BrowserListGrid.ItemsSource = _filteredEnvironments;
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // 页面初始化时可能触发，需要检查
            if (_allEnvironments == null) return;
            
            ApplyFilter();
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e)
        {
            // 页面初始化时可能触发，需要检查
            if (_allEnvironments == null) return;
            
            ApplyFilter();
        }

        private void BrowserCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = System.Windows.Media.Brushes.Blue;
            }
        }

        private void BrowserCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDD, 0xDD, 0xDD));
            }
        }

        private void BrowserCard_Checked(object sender, RoutedEventArgs e)
        {
            UpdateBatchOperationBar();
        }

        private void BrowserCard_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateBatchOperationBar();
        }

        private void BrowserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBatchOperationBar();
        }

        #endregion

        #region 浏览器操作

        private async void CreateRandomBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "正在创建随机浏览器...";
                
                // 获取随机生成服务
                var randomGenerator = _host.Services.GetRequiredService<BrowserRandomGenerator>();
                
                // 生成随机浏览器
                var (browser, profile) = await randomGenerator.GenerateRandomBrowserAsync();
                
                // 保存到数据库
                _db.FingerprintProfiles.Add(profile);
                await _db.SaveChangesAsync();
                
                browser.FingerprintProfileId = profile.Id;
                _db.BrowserEnvironments.Add(browser);
                await _db.SaveChangesAsync();
                
                // 刷新列表
                LoadEnvironments();
                
                StatusText.Text = $"随机浏览器 '{browser.Name}' 创建成功！";
                MessageBox.Show($"随机浏览器创建成功！\n\n名称: {browser.Name}\n引擎: {browser.Engine}\n系统: {browser.OS}", 
                    "创建成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Create random browser failed: {ex.Message}", ex.StackTrace);
                StatusText.Text = $"创建失败: {ex.Message}";
                MessageBox.Show($"创建随机浏览器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BrowserEnvironmentEditorDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                LoadEnvironments();
                StatusText.Text = "浏览器创建成功";
            }
        }

        private void LaunchBrowser_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is Button btn && btn.Tag is BrowserEnvironmentViewModel)
            {
                vm = btn.Tag as BrowserEnvironmentViewModel;
            }
            else if (sender is MenuItem && BrowserListGrid.SelectedItem is BrowserEnvironmentViewModel)
            {
                vm = BrowserListGrid.SelectedItem as BrowserEnvironmentViewModel;
            }

            if (vm == null)
            {
                MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LaunchEnvironment(vm.Environment);
        }

        private async void LaunchEnvironment(BrowserEnvironment env)
        {
            try
            {
                StatusText.Text = $"正在启动 {env.Name}...";
                
                // 从数据库读取关联的 Profile（新编辑器会自动创建）
                var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == env.FingerprintProfileId);
                if (profile == null)
                {
                    MessageBox.Show("未找到指纹配置，请重新编辑浏览器", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // ⭐ 调试日志：检查 Profile 中的 Platform 和 UserAgent
                var uaPreview = profile.UserAgent != null && profile.UserAgent.Length > 50 
                    ? profile.UserAgent.Substring(0, 50) + "..." 
                    : profile.UserAgent ?? "(null)";
                _log.LogInfo("BrowserMgmt", $"Profile loaded: Platform={profile.Platform}, UserAgent={uaPreview}");

                string? userDataPath = null;
                if (env.EnablePersistence)
                {
                    userDataPath = _sessionSvc.InitializeSessionPath(env);
                }

                var fingerprintSvc = _host.Services.GetRequiredService<FingerprintService>();
                var logSvc = _host.Services.GetRequiredService<LogService>();
                var secretSvc = _host.Services.GetRequiredService<SecretService>();
                
                var controller = new BrowserControllerAdapter(logSvc, fingerprintSvc, secretSvc);
                
                // 根据 Engine 设置选择浏览器引擎
                // Firefox 和 Chromium 使用 Playwright，UndetectedChrome 使用 UndetectedChrome
                bool useUndetectedChrome = env.Engine?.Equals("UndetectedChrome", StringComparison.OrdinalIgnoreCase) ?? true;
                controller.SetUseUndetectedChrome(useUndetectedChrome);
                
                // 设置浏览器类型（用于 Playwright）
                if (!useUndetectedChrome)
                {
                    string browserType = env.Engine?.Equals("Firefox", StringComparison.OrdinalIgnoreCase) == true ? "firefox" : "chromium";
                    controller.SetBrowserType(browserType);
                }

                await controller.InitializeBrowserAsync(profile, proxy: null, headless: false, userDataPath: userDataPath, loadAutoma: false, environment: env);
                
                _sessionSvc.RecordLaunch(env.Id);
                
                // 根据引擎显示不同的状态信息
                string engineInfo = env.Engine switch
                {
                    "UndetectedChrome" => "🤖 UndetectedChrome（成功率 90-95%）",
                    "Firefox" => "🦊 Firefox",
                    "Chromium" => "🌐 Chromium",
                    _ => "🤖 UndetectedChrome（成功率 90-95%）"
                };
                StatusText.Text = $"浏览器 '{env.Name}' 已启动 | {engineInfo}";
                
                LoadEnvironments();
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Launch failed: {ex.Message}", ex.StackTrace);
                StatusText.Text = $"启动失败: {ex.Message}";
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditBrowser_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is Button btn && btn.Tag is BrowserEnvironmentViewModel)
            {
                vm = btn.Tag as BrowserEnvironmentViewModel;
            }
            else if (sender is MenuItem && BrowserListGrid.SelectedItem is BrowserEnvironmentViewModel)
            {
                vm = BrowserListGrid.SelectedItem as BrowserEnvironmentViewModel;
            }

            if (vm == null)
            {
                MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new BrowserEnvironmentEditorDialog(vm.Environment) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                LoadEnvironments();
                StatusText.Text = "浏览器更新成功";
            }
        }

        private void ViewFingerprint_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is Button btn && btn.Tag is BrowserEnvironmentViewModel)
            {
                vm = btn.Tag as BrowserEnvironmentViewModel;
            }
            else if (sender is MenuItem && BrowserListGrid.SelectedItem is BrowserEnvironmentViewModel)
            {
                vm = BrowserListGrid.SelectedItem as BrowserEnvironmentViewModel;
            }

            if (vm == null)
            {
                MessageBox.Show("请选择一个浏览器环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 从数据库读取关联的 Profile
                var profile = _db.FingerprintProfiles.FirstOrDefault(p => p.Id == vm.Environment.FingerprintProfileId);
                if (profile == null)
                {
                    MessageBox.Show("未找到指纹配置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 打开指纹信息窗口，传入 BrowserEnvironment 以获取 WebdriverMode
                var dialog = new BrowserFingerprintInfoDialog(profile, vm.Environment) { Owner = Window.GetWindow(this) };
                dialog.ShowDialog();
                
                StatusText.Text = $"已显示 '{vm.Name}' 的指纹信息";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Failed to show fingerprint info: {ex.Message}", ex.StackTrace);
                MessageBox.Show($"打开指纹信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowBrowserMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BrowserEnvironmentViewModel vm)
            {
                var menu = new ContextMenu();
                menu.Items.Add(new MenuItem { Header = "🔄 更换指纹", Tag = vm });
                menu.Items.Add(new MenuItem { Header = "📁 移动到分组", Tag = vm });
                menu.Items.Add(new MenuItem { Header = "🗑️ 清除会话", Tag = vm });
                menu.Items.Add(new Separator());
                menu.Items.Add(new MenuItem { Header = "🗑️ 删除", Tag = vm });
                
                foreach (var obj in menu.Items)
                {
                    if (obj is MenuItem item)
                    {
                        if (item.Header.ToString().Contains("更换"))
                            item.Click += ChangeProfile_Click;
                        else if (item.Header.ToString().Contains("移动"))
                            item.Click += MoveToGroup_Click;
                        else if (item.Header.ToString().Contains("清除"))
                            item.Click += ClearSession_Click;
                        else if (item.Header.ToString().Contains("删除"))
                            item.Click += DeleteBrowser_Click;
                    }
                }
                
                menu.PlacementTarget = btn;
                menu.IsOpen = true;
            }
        }

        private void DeleteBrowser_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is MenuItem item && item.Tag is BrowserEnvironmentViewModel)
            {
                vm = item.Tag as BrowserEnvironmentViewModel;
            }
            else if (BrowserListGrid.SelectedItem is BrowserEnvironmentViewModel)
            {
                vm = BrowserListGrid.SelectedItem as BrowserEnvironmentViewModel;
            }

            if (vm == null) return;

            var result = MessageBox.Show($"确定删除浏览器 '{vm.Name}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _svc.DeleteEnvironment(vm.Environment.Id);
                    LoadEnvironments();
                    StatusText.Text = "浏览器已删除";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangeProfile_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is MenuItem item && item.Tag is BrowserEnvironmentViewModel)
            {
                vm = item.Tag as BrowserEnvironmentViewModel;
            }

            if (vm == null) return;

            var profiles = _db.FingerprintProfiles.OrderByDescending(p => p.UpdatedAt).ToList();
            if (!profiles.Any())
            {
                MessageBox.Show("没有可用的指纹配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SelectProfileDialog(profiles, vm.Environment.FingerprintProfileId) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.SelectedProfileId.HasValue)
            {
                try
                {
                    _svc.SwitchProfile(vm.Environment.Id, dialog.SelectedProfileId.Value);
                    LoadEnvironments();
                    StatusText.Text = "指纹配置已更换";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MoveToGroup_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is MenuItem item && item.Tag is BrowserEnvironmentViewModel)
            {
                vm = item.Tag as BrowserEnvironmentViewModel;
            }

            if (vm == null) return;

            var groups = _svc.GetAllGroups();
            var dialog = new MoveToGroupDialog(groups, vm.Environment.GroupId) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _svc.MoveEnvironmentToGroup(vm.Environment.Id, dialog.SelectedGroupId);
                    LoadEnvironments();
                    StatusText.Text = "浏览器已移动";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"移动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearSession_Click(object sender, RoutedEventArgs e)
        {
            BrowserEnvironmentViewModel vm = null;
            
            if (sender is MenuItem item && item.Tag is BrowserEnvironmentViewModel)
            {
                vm = item.Tag as BrowserEnvironmentViewModel;
            }

            if (vm == null) return;

            if (!_sessionSvc.HasSession(vm.Environment))
            {
                MessageBox.Show("该环境没有保存的会话数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定清除 '{vm.Name}' 的会话数据吗？", "确认清除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _sessionSvc.ClearSession(vm.Environment.Id);
                    StatusText.Text = "会话数据已清除";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 分组操作

        private void NewGroup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GroupEditDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _svc.CreateGroup(dialog.GroupName, dialog.GroupDescription);
                    LoadGroups();
                    StatusText.Text = "分组创建成功";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroupId == null || _selectedGroupId == -1)
            {
                MessageBox.Show("请选择一个分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
            if (group == null) return;

            var dialog = new GroupEditDialog(group) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _svc.UpdateGroup(group.Id, dialog.GroupName, dialog.GroupDescription);
                    LoadGroups();
                    StatusText.Text = "分组更新成功";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroupId == null || _selectedGroupId == -1)
            {
                MessageBox.Show("请选择一个分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var group = _db.BrowserGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
            if (group == null) return;

            var result = MessageBox.Show($"确定删除分组 '{group.Name}' 吗？\n该分组下的浏览器将变为未分组。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _svc.DeleteGroup(group.Id);
                    LoadData();
                    StatusText.Text = "分组已删除";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 批量操作

        private void BatchLaunch_Click(object sender, RoutedEventArgs e)
        {
            var selected = _filteredEnvironments.Where(e => e.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("请先选择浏览器", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定启动 {selected.Count} 个浏览器吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var vm in selected)
                {
                    LaunchEnvironment(vm.Environment);
                }
            }
        }

        private void BatchChangeProfile_Click(object sender, RoutedEventArgs e)
        {
            var selected = _filteredEnvironments.Where(e => e.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("请先选择浏览器", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var profiles = _db.FingerprintProfiles.OrderByDescending(p => p.UpdatedAt).ToList();
            if (!profiles.Any())
            {
                MessageBox.Show("没有可用的指纹配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SelectProfileDialog(profiles, null) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.SelectedProfileId.HasValue)
            {
                try
                {
                    foreach (var vm in selected)
                    {
                        _svc.SwitchProfile(vm.Environment.Id, dialog.SelectedProfileId.Value);
                    }
                    LoadEnvironments();
                    StatusText.Text = $"已为 {selected.Count} 个浏览器更换指纹配置";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批量更换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BatchMove_Click(object sender, RoutedEventArgs e)
        {
            var selected = _filteredEnvironments.Where(e => e.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("请先选择浏览器", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var groups = _svc.GetAllGroups();
            var dialog = new MoveToGroupDialog(groups, null) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    foreach (var vm in selected)
                    {
                        _svc.MoveEnvironmentToGroup(vm.Environment.Id, dialog.SelectedGroupId);
                    }
                    LoadEnvironments();
                    StatusText.Text = $"已移动 {selected.Count} 个浏览器";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批量移动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BatchDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = _filteredEnvironments.Where(e => e.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("请先选择浏览器", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定删除 {selected.Count} 个浏览器吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var vm in selected)
                    {
                        _svc.DeleteEnvironment(vm.Environment.Id);
                    }
                    LoadEnvironments();
                    StatusText.Text = $"已删除 {selected.Count} 个浏览器";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批量删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _filteredEnvironments)
            {
                vm.IsSelected = false;
            }
            UpdateBatchOperationBar();
        }

        #endregion

        #region 其他操作

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            StatusText.Text = "已刷新";
        }

        private void OpenCloudflareTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var testWindow = new WebScraperApp.Views.CloudflareTestWindow
                {
                    Owner = Window.GetWindow(this)
                };
                testWindow.Show();
                _log.LogInfo("BrowserMgmt", "Cloudflare test window opened");
                StatusText.Text = "已打开 Cloudflare 测试窗口";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Failed to open Cloudflare test window: {ex.Message}", ex.StackTrace);
                MessageBox.Show($"打开测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 测试工具

        private void CompareFingerprints_Click(object sender, RoutedEventArgs e)
        {
            _log.LogInfo("BrowserMgmt", "========== Fingerprint Comparison Tool ==========");
            StatusText.Text = "打开指纹对比工具...";
            MessageBox.Show("指纹对比功能（待实现）\n\n对比真实浏览器和 Playwright 的所有指纹特征", "指纹对比", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void LaunchMVP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _log.LogInfo("BrowserMgmt", "========== Starting Cloudflare Test Browser ==========");
                StatusText.Text = "正在启动 Cloudflare 测试浏览器...";

                // 询问用户使用哪个浏览器
                var result = MessageBox.Show(
                    "Firefox 已证实可以绕过 Cloudflare 的 TLS 指纹检测！\n\n" +
                    "选择浏览器：\n" +
                    "• 是(Y) = Firefox（推荐，已验证可通过）\n" +
                    "• 否(N) = Chrome（TLS 指纹可能被检测）\n" +
                    "• 取消 = 取消启动",
                    "选择浏览器引擎",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    _log.LogInfo("BrowserMgmt", "Cloudflare test cancelled by user");
                    StatusText.Text = "已取消";
                    return;
                }

                bool useFirefox = (result == MessageBoxResult.Yes);
                _log.LogInfo("BrowserMgmt", $"Selected browser: {(useFirefox ? "Firefox" : "Chrome")}");

                var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                
                Microsoft.Playwright.IBrowser browser;
                
                if (useFirefox)
                {
                    _log.LogInfo("BrowserMgmt", "🦊 Launching Firefox (TLS fingerprint bypass confirmed)");
                    browser = await playwright.Firefox.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                    {
                        Headless = false
                    });
                }
                else
                {
                    _log.LogInfo("BrowserMgmt", "🌐 Launching Chrome (TLS fingerprint may be detected)");
                    browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                    {
                        Headless = false,
                        Channel = "chrome"
                    });
                }

                var contextOptions = new Microsoft.Playwright.BrowserNewContextOptions
                {
                    Locale = "zh-CN",
                    TimezoneId = "Asia/Shanghai",
                    ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1280, Height = 720 },
                    DeviceScaleFactor = 1
                };

                if (useFirefox)
                {
                    contextOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0";
                    contextOptions.ExtraHTTPHeaders = new Dictionary<string, string>
                    {
                        ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8"
                    };
                    _log.LogInfo("BrowserMgmt", "Firefox User-Agent configured");
                }
                else
                {
                    contextOptions.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
                    contextOptions.ExtraHTTPHeaders = new Dictionary<string, string>
                    {
                        ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8",
                        ["sec-ch-ua"] = "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"141\", \"Google Chrome\";v=\"141\"",
                        ["sec-ch-ua-mobile"] = "?0",
                        ["sec-ch-ua-platform"] = "\"Windows\""
                    };
                    _log.LogInfo("BrowserMgmt", "Chrome User-Agent and Client Hints configured");
                }

                var context = await browser.NewContextAsync(contextOptions);

                // 加载防检测脚本
                var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
                if (System.IO.File.Exists(scriptPath))
                {
                    var antiDetectionScript = await System.IO.File.ReadAllTextAsync(scriptPath);
                    await context.AddInitScriptAsync(antiDetectionScript);
                    _log.LogInfo("BrowserMgmt", $"✅ Anti-detection script loaded: {scriptPath}");
                }
                else
                {
                    _log.LogWarn("BrowserMgmt", $"Anti-detection script not found: {scriptPath}");
                }

                var page = await context.NewPageAsync();
                
                _log.LogInfo("BrowserMgmt", "Navigating to Cloudflare test site: https://nowsecure.nl");
                StatusText.Text = "正在导航到测试网站...";
                
                await page.GotoAsync("https://nowsecure.nl", new Microsoft.Playwright.PageGotoOptions
                {
                    Timeout = 30000,
                    WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded
                });
                
                _log.LogInfo("BrowserMgmt", "✅ Cloudflare test browser launched successfully");
                StatusText.Text = "✅ Cloudflare 测试浏览器已启动";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Failed to launch Cloudflare test browser: {ex.Message}", ex.StackTrace);
                StatusText.Text = $"❌ 启动失败: {ex.Message}";
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LaunchFirefox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _log.LogInfo("BrowserMgmt", "========== Starting Firefox Test Browser ==========");
                StatusText.Text = "正在启动 Firefox 测试浏览器...";

                // 生成测试用的指纹配置
                var profile = new FingerprintProfile
                {
                    Name = "Firefox Test Profile",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                    Platform = "Win32",
                    Locale = "zh-CN",
                    Timezone = "Asia/Shanghai",
                    AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8",
                    HardwareConcurrency = 8,
                    DeviceMemory = 8
                };
                
                _log.LogInfo("BrowserMgmt", $"Generated test profile: {profile.Name}");
                _log.LogInfo("BrowserMgmt", $"User-Agent: {profile.UserAgent}");

                // 打开指纹信息窗口
                var fingerprintDialog = new Dialogs.BrowserFingerprintInfoDialog(profile);
                fingerprintDialog.Show();
                _log.LogInfo("BrowserMgmt", "✅ Fingerprint info dialog opened");

                var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                
                _log.LogInfo("BrowserMgmt", "🦊 Launching Firefox");
                var browser = await playwright.Firefox.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                {
                    Headless = false
                });

                var contextOptions = new Microsoft.Playwright.BrowserNewContextOptions
                {
                    Locale = profile.Locale,
                    TimezoneId = profile.Timezone,
                    ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1280, Height = 720 },
                    UserAgent = profile.UserAgent,
                    ExtraHTTPHeaders = new Dictionary<string, string>
                    {
                        ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8"
                    }
                };

                var context = await browser.NewContextAsync(contextOptions);
                var page = await context.NewPageAsync();
                
                _log.LogInfo("BrowserMgmt", "✅ Firefox test browser launched successfully");
                StatusText.Text = "✅ Firefox 测试浏览器已启动（指纹信息窗口已打开）";
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Failed to launch Firefox test browser: {ex.Message}", ex.StackTrace);
                StatusText.Text = $"❌ 启动失败: {ex.Message}";
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LaunchUndetectedChrome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _log.LogInfo("BrowserMgmt", "========== Starting Undetected Chrome Test Browser ==========");
                StatusText.Text = "正在启动 Undetected Chrome 测试浏览器...";

                // 生成测试用的指纹配置
                var profile = new FingerprintProfile
                {
                    Name = "UndetectedChrome Test Profile",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
                    Platform = "Win32",
                    Locale = "zh-CN",
                    Timezone = "Asia/Shanghai",
                    AcceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8",
                    LanguagesJson = "[\"zh-CN\",\"zh\",\"en-US\",\"en\"]",
                    HardwareConcurrency = 8,
                    DeviceMemory = 8,
                    SecChUa = "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"141\", \"Google Chrome\";v=\"141\"",
                    SecChUaMobile = "?0",
                    SecChUaPlatform = "\"Windows\"",
                    ViewportWidth = 1280,
                    ViewportHeight = 720
                };
                
                _log.LogInfo("BrowserMgmt", $"Generated test profile: {profile.Name}");
                _log.LogInfo("BrowserMgmt", $"User-Agent: {profile.UserAgent}");

                // 打开指纹信息窗口
                var fingerprintDialog = new Dialogs.BrowserFingerprintInfoDialog(profile);
                fingerprintDialog.Show();
                _log.LogInfo("BrowserMgmt", "✅ Fingerprint info dialog opened");

                // 使用 UndetectedChromeLauncher
                var launcher = new UndetectedChromeLauncher(_log);
                
                _log.LogInfo("BrowserMgmt", "🤖 Launching Undetected Chrome (Selenium + undetected-chromedriver)");
                _log.LogInfo("BrowserMgmt", "This will download ChromeDriver automatically if needed...");
                
                StatusText.Text = "正在下载 ChromeDriver 并启动浏览器...";
                
                await launcher.LaunchAsync(
                    profile: profile,
                    userDataPath: null,
                    headless: false,
                    proxy: null,
                    environment: null
                );
                
                _log.LogInfo("BrowserMgmt", "✅ Undetected Chrome test browser launched successfully");
                StatusText.Text = "✅ Undetected Chrome 测试浏览器已启动（指纹信息窗口已打开）";
                
                MessageBox.Show(
                    "✅ Undetected Chrome 已启动！\n\n" +
                    "特点：\n" +
                    "• 使用真实 Chrome 的 TLS 指纹（包含 GREASE）\n" +
                    "• 修补了 ChromeDriver 的检测特征\n" +
                    "• 移除了自动化标志\n" +
                    "• 成功率 90-95%\n\n" +
                    "指纹信息窗口已打开，可以查看详细配置。",
                    "Undetected Chrome",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserMgmt", $"Failed to launch Undetected Chrome: {ex.Message}", ex.StackTrace);
                StatusText.Text = $"❌ 启动失败: {ex.Message}";
                MessageBox.Show(
                    $"启动失败：{ex.Message}\n\n" +
                    "可能的原因：\n" +
                    "• ChromeDriver 下载失败（需要网络连接）\n" +
                    "• Chrome 浏览器未安装\n" +
                    "• 权限不足\n\n" +
                    "详细错误信息请查看日志。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }

    // ViewModel 类
    public class BrowserEnvironmentViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected;

        public BrowserEnvironment Environment { get; set; }

        public int Id => Environment.Id;
        public string Name => Environment.Name;
        public string? Engine => Environment.Engine;
        public string? OS => Environment.OS;
        public string? Notes => Environment.Notes;
        public int LaunchCount => Environment.LaunchCount;
        public bool EnablePersistence => Environment.EnablePersistence;
        public DateTime CreatedAt => Environment.CreatedAt;
        public FingerprintProfile? FingerprintProfile => Environment.FingerprintProfile;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public BrowserEnvironmentViewModel(BrowserEnvironment environment)
        {
            Environment = environment;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
