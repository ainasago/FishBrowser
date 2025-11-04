using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using WpfApplication = System.Windows.Application;

namespace FishBrowser.WPF.Views;

public partial class ProxyPoolView : Page
{
    private readonly ProxyCatalogService _catalog;
    private readonly ProxyHealthService _health;
    private readonly LogService _log;

    private ObservableCollection<ProxyPool> _pools = new();
    private ObservableCollection<ProxyServer> _members = new();
    private List<ProxyServer> _allMembers = new();
    private ObservableCollection<SelectableProxy> _ungrouped = new();
    private ProxyPool? _currentPool;
    private int _memPageIndex = 0;
    private const int MemPageSize = 100;
    private int _ungPageIndex = 0;
    private int _ungTotal = 0;
    private const int UngPageSize = 100;

    // 记忆导入策略选择
    private static ImportExistingStrategy _lastImportStrategy = ImportExistingStrategy.Attach;
    private static bool _lastMoveDoNotEnable = false;
    private static bool _lastExportReport = true;

    public ProxyPoolView()
    {
        InitializeComponent();

        var host = WpfApplication.Current.Resources["Host"] as IHost;
        _catalog = host?.Services.GetRequiredService<ProxyCatalogService>() ?? throw new InvalidOperationException("ProxyCatalogService not found");
        _health = host?.Services.GetRequiredService<ProxyHealthService>() ?? throw new InvalidOperationException("ProxyHealthService not found");
        _log = host?.Services.GetRequiredService<LogService>() ?? throw new InvalidOperationException("LogService not found");

        PoolsList.ItemsSource = _pools;
        MembersGrid.ItemsSource = _members;
        UngroupedGrid.ItemsSource = _ungrouped;

        LoadPools();
        LoadUngroupedPage();
    }

    private void ExportMembers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPool == null)
            {
                MessageBox.Show("请先选择左侧的代理池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 确保 _allMembers 已加载
            if (_allMembers == null || _allMembers.Count == 0)
            {
                _allMembers = _catalog.GetProxiesByPool(_currentPool.Id);
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = $"pool_{_currentPool.Id}_{_currentPool.Name}_members.csv",
                Title = "导出成员（CSV）"
            };
            if (sfd.ShowDialog() == true)
            {
                var sb = new System.Text.StringBuilder();
                // header
                sb.AppendLine("protocol,address,port,username,password,label");
                foreach (var p in _allMembers)
                {
                    string esc(string? s) => (s ?? string.Empty).Replace("\"", "\"\"");
                    sb.Append('"').Append(esc(p.Protocol)).Append('"').Append(',')
                      .Append('"').Append(esc(p.Address)).Append('"').Append(',')
                      .Append(p.Port).Append(',')
                      .Append('"').Append(esc(p.Username)).Append('"').Append(',')
                      .Append('"').Append(esc(string.Empty)).Append('"').Append(',')
                      .Append('"').Append(esc(p.Label)).Append('"')
                      .AppendLine();
                }
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show($"已导出 {_allMembers.Count} 条到:\n{sfd.FileName}\n格式：protocol,address,port,username,password,label", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"ExportMembers failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPools()
    {
        try
        {
            _pools.Clear();
            var list = _catalog.GetPools();
            foreach (var p in list) _pools.Add(p);
            if (_pools.Count > 0)
            {
                PoolsList.SelectedIndex = 0;
            }
            _log.LogInfo("PoolUI", $"Loaded {_pools.Count} pools");
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"LoadPools failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载代理池失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadMembers()
    {
        try
        {
            _members.Clear();
            _allMembers.Clear();
            _memPageIndex = 0;
            if (_currentPool == null) return;
            _allMembers = _catalog.GetProxiesByPool(_currentPool.Id);
            LoadMembersPage();
            _log.LogInfo("PoolUI", $"Pool {_currentPool.Name} members: {_allMembers.Count}");
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"LoadMembers failed: {ex.Message}", ex.StackTrace);
        }
    }

    private void LoadMembersPage()
    {
        _members.Clear();
        var total = _allMembers.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)MemPageSize));
        _memPageIndex = Math.Min(Math.Max(0, _memPageIndex), totalPages - 1);
        var pageItems = _allMembers
            .Skip(_memPageIndex * MemPageSize)
            .Take(MemPageSize)
            .ToList();
        foreach (var m in pageItems) _members.Add(m);
        MembersPageInfo.Text = $"第 {_memPageIndex + 1}/{totalPages} 页，共 {total} 条";
    }

    private void LoadUngroupedPage()
    {
        try
        {
            _ungrouped.Clear();
            var query = string.IsNullOrWhiteSpace(UngroupedSearchBox.Text) ? null : UngroupedSearchBox.Text.Trim();
            _ungTotal = _catalog.CountUngroupedProxies(query);
            var list = _catalog.GetUngroupedProxiesPaged(_ungPageIndex * UngPageSize, UngPageSize, query);
            foreach (var p in list)
            {
                _ungrouped.Add(new SelectableProxy
                {
                    Id = p.Id,
                    Address = p.Address,
                    Port = p.Port,
                    Protocol = p.Protocol,
                    Label = string.IsNullOrWhiteSpace(p.Label) ? $"{p.Address}:{p.Port}" : p.Label
                });
            }
            var totalPages = Math.Max(1, (int)Math.Ceiling(_ungTotal / (double)UngPageSize));
            UngroupedPageInfo.Text = $"第 {_ungPageIndex + 1}/{totalPages} 页，共 {_ungTotal} 条";
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"LoadUngroupedPage failed: {ex.Message}", ex.StackTrace);
        }
    }

    private void AddProxy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPool == null)
            {
                MessageBox.Show("请先选择左侧的代理池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dialog = new SelectProxiesDialog(_catalog) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.SelectedProxyIds.Any())
            {
                _catalog.AssignProxiesToPool(dialog.SelectedProxyIds, _currentPool.Id);
                LoadMembers();
                MessageBox.Show($"已添加 {dialog.SelectedProxyIds.Count} 个代理到池", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"AddProxy failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"添加代理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPool == null)
            {
                MessageBox.Show("请先选择左侧的代理池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建文件选择对话框
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV/文本文件 (*.csv;*.txt)|*.csv;*.txt|CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "选择代理列表文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // 选择遇到“已存在但在其他池”时的处理策略
                var strategy = _lastImportStrategy;
                var stratDlg = new ImportExistingStrategyDialog(_lastImportStrategy, _lastMoveDoNotEnable, _lastExportReport) { Owner = Window.GetWindow(this) };
                if (stratDlg.ShowDialog() == true)
                {
                    strategy = stratDlg.SelectedStrategy;
                    _lastImportStrategy = strategy;
                    _lastMoveDoNotEnable = stratDlg.MoveDoNotEnable;
                    _lastExportReport = stratDlg.ExportReport;
                }

                var path = openFileDialog.FileName;
                int createdCount = 0;
                int attachedExistingCount = 0; // 包含移动
                int skippedExistingCount = 0;
                int duplicateInFileCount = 0;
                int duplicateInDbSamePoolCount = 0;
                int failCount = 0;

                // 预加载现有代理建立索引（protocol|address|port）
                var existing = _catalog.GetAll();
                var existingIndex = new Dictionary<string, ProxyServer>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in existing)
                {
                    var key = $"{(p.Protocol ?? "http").ToLowerInvariant()}|{NormalizeHost(p.Address)}|{p.Port}";
                    if (!existingIndex.ContainsKey(key)) existingIndex[key] = p; // 若数据库有重复，保留第一条
                }
                var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // 差异清单
                var createdList = new List<string>();
                var attachedList = new List<string>();
                var skippedList = new List<string>();
                var fileDupList = new List<string>();
                var alreadyInPoolList = new List<string>();
                var failedList = new List<string>();

                if (System.IO.Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    // CSV: protocol,address,port,username,password,label
                    var lines = System.IO.File.ReadAllLines(path);
                    if (lines.Length > 0) {
                        // optional header
                        int start = 0;
                        var header = lines[0].Trim().ToLowerInvariant();
                        if (header.Contains("protocol") && header.Contains("address") && header.Contains("port")) start = 1;
                        for (int i = start; i < lines.Length; i++)
                        {
                            var row = lines[i];
                            if (string.IsNullOrWhiteSpace(row)) continue;
                            try
                            {
                                var cols = ParseCsvRow(row);
                                if (cols.Count < 3) { failCount++; continue; }
                                var protocol = (cols.ElementAtOrDefault(0) ?? "http").Trim().ToLowerInvariant();
                                var address = NormalizeHost(cols.ElementAtOrDefault(1) ?? "");
                                var portStr = cols.ElementAtOrDefault(2) ?? "0";
                                var username = cols.ElementAtOrDefault(3);
                                var password = cols.ElementAtOrDefault(4);
                                var label = cols.ElementAtOrDefault(5);
                                if (!int.TryParse(portStr, out var port) || string.IsNullOrWhiteSpace(address)) { failCount++; continue; }
                                var key = $"{protocol}|{address}|{port}";
                                if (!seenInFile.Add(key)) { duplicateInFileCount++; fileDupList.Add(key); continue; }
                                if (existingIndex.TryGetValue(key, out var existed))
                                {
                                    if (existed.PoolId != _currentPool.Id)
                                    {
                                        if (strategy == ImportExistingStrategy.Skip)
                                        {
                                            skippedExistingCount++;
                                            skippedList.Add(key);
                                        }
                                        else
                                        {
                                            // 附加/移动：由于单 PoolId，效果等同“移动”
                                            existed.PoolId = _currentPool.Id;
                                            if (!existed.Enabled && !_lastMoveDoNotEnable) existed.Enabled = true;
                                            _catalog.Update(existed);
                                            attachedExistingCount++;
                                            attachedList.Add(key);
                                        }
                                    }
                                    else
                                    {
                                        duplicateInDbSamePoolCount++;
                                        alreadyInPoolList.Add(key);
                                    }
                                }
                                else
                                {
                                    var proxy = _catalog.Create(label ?? $"{address}:{port}", protocol, address, port, username, password);
                                    proxy.PoolId = _currentPool.Id;
                                    _catalog.Update(proxy);
                                    existingIndex[key] = proxy;
                                    createdCount++;
                                    createdList.Add(key);
                                }
                            }
                            catch { failCount++; failedList.Add(row); }
                        }
                    }
                }
                else
                {
                    // TXT: support multiple line formats
                    var lines = System.IO.File.ReadAllLines(path);
                    foreach (var raw in lines)
                    {
                        var line = raw.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        try
                        {
                            // split label
                            string main = line;
                            string? label = null;
                            var spaceIdx = line.IndexOf(' ');
                            if (spaceIdx > 0)
                            {
                                main = line.Substring(0, spaceIdx).Trim();
                                label = line.Substring(spaceIdx + 1).Trim();
                                if (label.Length == 0) label = null;
                            }

                            // ensure scheme for Uri parsing
                            string protocol = "http";
                            string? username = null;
                            string? password = null;
                            string address = "";
                            int port = 0;

                            string candidate = main.Contains("://") ? main : $"http://{main}";
                            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
                            {
                                protocol = uri.Scheme.ToLowerInvariant();
                                address = uri.Host;
                                port = uri.Port;
                                if (!string.IsNullOrEmpty(uri.UserInfo))
                                {
                                    var up = uri.UserInfo.Split(':');
                                    username = up.ElementAtOrDefault(0);
                                    password = up.ElementAtOrDefault(1);
                                }
                            }
                            else
                            {
                                // fallback: address:port
                                var ap = main.Split(':');
                                if (ap.Length >= 2 && int.TryParse(ap[1], out var p))
                                {
                                    address = ap[0].Trim();
                                    port = p;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(address) || port <= 0)
                            {
                                failCount++;
                                failedList.Add(line);
                                continue;
                            }

                            protocol = (protocol ?? "http").ToLowerInvariant();
                            address = NormalizeHost(address);
                            var key = $"{protocol}|{address}|{port}";
                            if (!seenInFile.Add(key)) { duplicateInFileCount++; fileDupList.Add(key); continue; }
                            if (existingIndex.TryGetValue(key, out var existed))
                            {
                                if (existed.PoolId != _currentPool.Id)
                                {
                                    if (strategy == ImportExistingStrategy.Skip)
                                    {
                                        skippedExistingCount++;
                                        skippedList.Add(key);
                                    }
                                    else
                                    {
                                        existed.PoolId = _currentPool.Id;
                                        if (!existed.Enabled && !_lastMoveDoNotEnable) existed.Enabled = true;
                                        _catalog.Update(existed);
                                        attachedExistingCount++;
                                        attachedList.Add(key);
                                    }
                                }
                                else
                                {
                                    duplicateInDbSamePoolCount++;
                                    alreadyInPoolList.Add(key);
                                }
                            }
                            else
                            {
                                var proxy = _catalog.Create(label ?? $"{address}:{port}", protocol, address, port, username, password);
                                proxy.PoolId = _currentPool.Id;
                                _catalog.Update(proxy);
                                existingIndex[key] = proxy;
                                createdCount++;
                                createdList.Add(key);
                            }
                        }
                        catch { failCount++; failedList.Add(line); }
                    }
                }

                LoadPools();
                PoolsList.SelectedValue = _currentPool.Id;
                MessageBox.Show($"导入完成！\n新建: {createdCount}\n移动/附加到当前池: {attachedExistingCount}\n跳过(其他池): {skippedExistingCount}\n文件内重复: {duplicateInFileCount}\n数据库已在当前池: {duplicateInDbSamePoolCount}\n失败: {failCount}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                if (_lastExportReport)
                {
                    if (MessageBox.Show("是否保存差异报告？", "导入完成", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var sfd = new Microsoft.Win32.SaveFileDialog
                        {
                            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                            FileName = $"import_diff_pool_{_currentPool.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                            Title = "保存差异报告"
                        };
                        if (sfd.ShowDialog() == true)
                        {
                            var sb = new System.Text.StringBuilder();
                            void WriteSection(string title, IEnumerable<string> items)
                            {
                                sb.AppendLine($"[{title}] ({items.Count()})");
                                foreach (var it in items) sb.AppendLine(it);
                                sb.AppendLine();
                            }
                            WriteSection("新建", createdList);
                            WriteSection("移动/附加到当前池", attachedList);
                            WriteSection("跳过(其他池)", skippedList);
                            WriteSection("数据库已在当前池", alreadyInPoolList);
                            WriteSection("文件内重复", fileDupList);
                            WriteSection("失败行", failedList);
                            System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                            MessageBox.Show("差异报告已保存", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"Failed to import proxies: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 简单 CSV 解析器，支持带引号字段
    private static List<string> ParseCsvRow(string row)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(row)) return result;
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < row.Length; i++)
        {
            var c = row[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < row.Length && row[i + 1] == '"') { sb.Append('"'); i++; }
                    else { inQuotes = false; }
                }
                else sb.Append(c);
            }
            else
            {
                if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else if (c == '"') { inQuotes = true; }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    // 归一化主机名：
    // - 去除方括号 [ ]
    // - 小写
    // - 若可解析为 IP：使用 IPAddress.ToString()（IPv4 去前导零，IPv6 规范化）
    // - 否则保留小写主机名
    private static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;
        host = host.Trim();
        if (host.StartsWith("[") && host.EndsWith("]") && host.Length > 2)
            host = host.Substring(1, host.Length - 2);
        host = host.ToLowerInvariant();
        if (IPAddress.TryParse(host, out var ip))
            return ip.ToString();
        return host;
    }

    private enum ImportExistingStrategy { Attach, Skip, Move }

    private class ImportExistingStrategyDialog : Window
    {
        public ImportExistingStrategy SelectedStrategy { get; private set; } = ImportExistingStrategy.Attach;
        public bool MoveDoNotEnable { get; private set; }
        public bool ExportReport { get; private set; }

        public ImportExistingStrategyDialog(ImportExistingStrategy defaultStrategy, bool moveDoNotEnable, bool exportReport)
        {
            Title = "导入冲突处理";
            Width = 420; Height = 260; WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize;
            var root = new DockPanel { Margin = new Thickness(16) };
            var panel = new StackPanel { Margin = new Thickness(0,0,0,8) };
            panel.Children.Add(new TextBlock { Text = "遇到\"已存在但在其他池\"时如何处理？", Margin = new Thickness(0,0,0,8) });
            var rbAttach = new RadioButton { Content = "附加/移动到当前池（解绑原池并加入当前池）", Margin = new Thickness(0,0,0,4) };
            var rbSkip = new RadioButton { Content = "跳过（保持在原池）", Margin = new Thickness(0,0,0,4) };
            var rbMove = new RadioButton { Content = "移动到当前池（解绑原池）", Margin = new Thickness(0,0,0,4) };
            // 选择默认策略
            rbAttach.IsChecked = defaultStrategy == ImportExistingStrategy.Attach || defaultStrategy == ImportExistingStrategy.Move;
            rbSkip.IsChecked = defaultStrategy == ImportExistingStrategy.Skip;
            rbMove.IsChecked = defaultStrategy == ImportExistingStrategy.Move;
            panel.Children.Add(rbAttach); panel.Children.Add(rbSkip); panel.Children.Add(rbMove);

            // 高级选项
            var adv = new StackPanel { Margin = new Thickness(0,12,0,0) };
            var cbDoNotEnable = new CheckBox { Content = "移动时不启用（保持禁用）", IsChecked = moveDoNotEnable };
            var cbExport = new CheckBox { Content = "导入后提示保存差异报告", IsChecked = exportReport };
            adv.Children.Add(cbDoNotEnable);
            adv.Children.Add(cbExport);
            panel.Children.Add(adv);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "确定", Width = 80, IsDefault = true, Margin = new Thickness(8,0,0,0) };
            var cancel = new Button { Content = "取消", Width = 80, IsCancel = true, Margin = new Thickness(8,0,0,0) };
            ok.Click += (_, __) => {
                if (rbSkip.IsChecked == true) SelectedStrategy = ImportExistingStrategy.Skip;
                else if (rbMove.IsChecked == true) SelectedStrategy = ImportExistingStrategy.Move;
                else SelectedStrategy = ImportExistingStrategy.Attach;
                MoveDoNotEnable = cbDoNotEnable.IsChecked == true;
                ExportReport = cbExport.IsChecked == true;
                DialogResult = true; Close();
            };
            cancel.Click += (_, __) => { DialogResult = false; Close(); };
            btns.Children.Add(ok); btns.Children.Add(cancel);
            DockPanel.SetDock(panel, Dock.Top); DockPanel.SetDock(btns, Dock.Bottom);
            root.Children.Add(panel); root.Children.Add(btns);
            Content = root;
        }
    }

    private async void HealthCheck_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_members.Count == 0)
            {
                MessageBox.Show("没有代理可检查", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var proxy in _members)
            {
                await _health.QuickProbeAsync(proxy);
            }
            MessageBox.Show("健康检查完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"Failed to perform health check: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"健康检查失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        // 可选：实现物理删除成员（当前不做删除以避免误删，交由通用代理管理页处理）
        MessageBox.Show("删除操作请在代理管理页面进行。此处仅管理池成员关系（添加/移除）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PoolsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentPool = PoolsList.SelectedItem as ProxyPool;
        LoadMembers();
    }

    private void AddPool_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddPoolDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            var pool = _catalog.CreatePool(dlg.PoolName, dlg.Strategy, dlg.OptionsJson);
            LoadPools();
            PoolsList.SelectedValue = pool.Id;
        }
    }

    private void RenamePool_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPool == null) { MessageBox.Show("请先选择一个池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var name = Microsoft.VisualBasic.Interaction.InputBox("新名称：", "重命名池", _currentPool.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            _currentPool.Name = name.Trim();
            _catalog.Update(_currentPool);
            LoadPools();
        }
    }

    private void DeletePool_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPool == null) { MessageBox.Show("请先选择一个池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var members = _catalog.GetProxiesByPool(_currentPool.Id);
        var msg = $"确定删除池 '{_currentPool.Name}'？\n成员数量: {members.Count}\n影响：仅删除池并将成员设为未分组（不删除代理）。";
        if (MessageBox.Show(msg, "确认删除池", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _catalog.DeletePool(_currentPool.Id);
            _currentPool = null;
            LoadPools();
        }
    }

    private void RemoveFromPool_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPool == null) { MessageBox.Show("请先选择一个池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var selected = MembersGrid.SelectedItems.Cast<ProxyServer>().ToList();
        if (selected.Count == 0) { MessageBox.Show("请选择要移除的成员", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        foreach (var m in selected)
        {
            m.PoolId = null;
            _catalog.Update(m);
        }
        LoadPools();
        if (_currentPool != null) PoolsList.SelectedValue = _currentPool.Id;
    }

    private void MembersPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_memPageIndex > 0)
        {
            _memPageIndex--;
            LoadMembersPage();
        }
    }

    private void MembersNext_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_allMembers.Count / (double)MemPageSize));
        if (_memPageIndex < totalPages - 1)
        {
            _memPageIndex++;
            LoadMembersPage();
        }
    }

    private void UngroupedSearch_Click(object sender, RoutedEventArgs e)
    {
        _ungPageIndex = 0;
        LoadUngroupedPage();
    }

    private void UngroupedPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_ungPageIndex > 0)
        {
            _ungPageIndex--;
            LoadUngroupedPage();
        }
    }

    private void UngroupedNext_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_ungTotal / (double)UngPageSize));
        if (_ungPageIndex < totalPages - 1)
        {
            _ungPageIndex++;
            LoadUngroupedPage();
        }
    }

    private void AddUngroupedToPool_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPool == null)
            {
                MessageBox.Show("请先选择左侧的代理池", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var ids = _ungrouped.Where(x => x.IsSelected).Select(x => x.Id).ToList();
            if (ids.Count == 0)
            {
                MessageBox.Show("请至少选择一个未分组代理", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _catalog.AssignProxiesToPool(ids, _currentPool.Id);
            LoadMembers();
            LoadUngroupedPage();
            MessageBox.Show($"已添加 {ids.Count} 个代理到池", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.LogError("PoolUI", $"AddUngroupedToPool failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"添加失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private class SelectableProxy
    {
        public int Id { get; set; }
        public bool IsSelected { get; set; }
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Protocol { get; set; } = "http";
        public string Label { get; set; } = string.Empty;
    }
}
