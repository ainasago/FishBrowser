# 浏览器会话持久化说明

## 工作原理

### Playwright 持久化上下文
使用 `LaunchPersistentContextAsync` 启动浏览器时，Playwright 会：
1. 在指定的 `userDataPath` 目录创建用户数据文件夹
2. 将所有浏览器状态保存到该目录
3. 关闭浏览器时自动将内存中的数据写入磁盘

### 保存的数据
- **Cookie**：登录状态、会话信息
- **LocalStorage/SessionStorage**：网站本地数据
- **IndexedDB**：网站数据库
- **浏览历史**：访问记录
- **书签**：收藏夹
- **扩展程序**：已安装的浏览器扩展及其数据
- **打开的标签页**：最后打开的页面（部分浏览器支持）
- **表单数据**：自动填充的表单信息
- **网站权限**：通知、位置、摄像头等权限设置

## 关键实现

### 1. 等待浏览器关闭
```csharp
// 在后台任务中等待浏览器关闭
_ = Task.Run(async () =>
{
    await controller.WaitForCloseAsync();
    // 此时会话数据已保存到磁盘
});
```

### 2. Context 关闭事件
```csharp
public async Task WaitForCloseAsync()
{
    if (_context != null)
    {
        // 等待用户关闭所有浏览器窗口
        await _context.WaitForCloseAsync();
    }
}
```

### 3. 会话目录结构
```
BrowserSessions/
└─ env_1_Dev-Chrome-CN/
   ├─ Default/
   │  ├─ Cookies              # Cookie 数据库
   │  ├─ History              # 浏览历史
   │  ├─ Local Storage/       # LocalStorage 数据
   │  ├─ Session Storage/     # SessionStorage 数据
   │  ├─ IndexedDB/           # IndexedDB 数据库
   │  ├─ Extensions/          # 扩展程序
   │  ├─ Preferences          # 浏览器设置
   │  ├─ Web Data             # 表单自动填充
   │  └─ ...
   └─ ...
```

## 使用流程

### 首次启动
1. 用户点击"启动"按钮
2. 系统创建 `BrowserSessions/env_{id}_{name}/` 目录
3. Playwright 启动持久化上下文
4. 用户浏览网站、登录账号、安装扩展
5. 用户关闭浏览器窗口
6. Playwright 自动保存所有数据到 userDataPath
7. 后台任务检测到关闭事件，记录日志

### 再次启动
1. 用户再次点击"启动"
2. 系统检测到已存在会话目录
3. Playwright 从 userDataPath 加载数据
4. 浏览器恢复：
   - 自动登录（Cookie 有效）
   - 扩展自动加载
   - 历史记录恢复
   - 设置保持不变

## 注意事项

### 1. 必须等待关闭
- **错误做法**：启动后立即返回，不等待关闭
- **正确做法**：在后台任务中调用 `WaitForCloseAsync()`

### 2. 不要手动调用 DisposeAsync
- 持久化模式下，让用户自然关闭浏览器
- 手动调用 `DisposeAsync()` 可能导致数据未完全写入

### 3. 会话目录权限
- 确保应用有读写权限
- Windows：通常在 `AppData` 或应用目录下
- 避免使用系统保护目录

### 4. 多实例冲突
- 同一 userDataPath 不能同时被多个浏览器实例使用
- 每个 BrowserEnvironment 必须有独立的会话目录

## 故障排查

### 问题：会话数据没有保存
**原因**：
- 浏览器被强制终止（进程被杀死）
- 没有等待 `WaitForCloseAsync()` 完成
- 会话目录权限不足

**解决**：
- 确保后台任务正常运行
- 检查日志：`Browser context closed, session data saved`
- 验证目录权限

### 问题：Cookie 过期
**原因**：
- 网站设置了短期 Cookie
- Cookie 有 HttpOnly/Secure 限制

**解决**：
- 定期重新登录
- 检查网站 Cookie 策略

### 问题：扩展未加载
**原因**：
- 扩展需要手动启用
- 扩展与浏览器版本不兼容

**解决**：
- 首次启动后手动启用扩展
- 使用兼容的扩展版本

## 性能优化

### 会话大小管理
- 定期清理历史记录（超过 30 天）
- 清除缓存文件（临时文件）
- 限制扩展数量

### 启动速度
- 首次启动较慢（创建目录）
- 后续启动快速（直接加载）
- 会话数据过大会影响启动速度

## 安全建议

### 敏感数据保护
- 会话目录包含登录凭证
- 建议加密存储（后续功能）
- 定期备份重要会话

### 多用户隔离
- 每个环境独立会话目录
- 不同用户账号使用不同环境
- 避免会话数据混淆

## 测试验证

### 验证步骤
1. 启动浏览器
2. 访问 https://www.browserscan.net/zh/
3. 打开多个标签页
4. 登录某个网站（如 GitHub）
5. 安装一个扩展（如 uBlock Origin）
6. 关闭浏览器
7. 再次启动
8. 验证：
   - ✅ 标签页恢复
   - ✅ 登录状态保持
   - ✅ 扩展自动加载
   - ✅ 历史记录存在

### 日志检查
```
[INFO] BrowserSession: Created session directory: BrowserSessions/env_1_Dev-Chrome-CN
[INFO] PlaywrightController: Browser initialized (mode: persistent, path: ...)
[INFO] PlaywrightController: Browser context closed, session data saved
[INFO] BrowserMgmt: Browser 'Dev-Chrome-CN' closed, session saved.
```

## 后续增强

- [ ] 会话自动备份
- [ ] 会话导入/导出
- [ ] 会话加密存储
- [ ] 会话大小限制
- [ ] 会话自动清理（按时间）
- [ ] 会话共享（团队协作）
- [ ] 标签页恢复增强
