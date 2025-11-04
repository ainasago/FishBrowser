# 测试 npm 命令是否可用

Write-Host "=== 测试 Node.js 和 npm 环境 ===" -ForegroundColor Green

# 测试 node
Write-Host "`n1. 测试 node 命令:" -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "   ✓ Node.js 版本: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Node.js 未安装或不在 PATH 中" -ForegroundColor Red
}

# 测试 npm
Write-Host "`n2. 测试 npm 命令:" -ForegroundColor Yellow
try {
    $npmVersion = npm --version
    Write-Host "   ✓ npm 版本: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "   ✗ npm 未安装或不在 PATH 中" -ForegroundColor Red
}

# 测试 npx
Write-Host "`n3. 测试 npx 命令:" -ForegroundColor Yellow
try {
    $npxVersion = npx --version
    Write-Host "   ✓ npx 版本: $npxVersion" -ForegroundColor Green
} catch {
    Write-Host "   ✗ npx 未安装或不在 PATH 中" -ForegroundColor Red
}

# 检查 npm 全局路径
Write-Host "`n4. npm 全局路径:" -ForegroundColor Yellow
try {
    $npmPrefix = npm config get prefix
    Write-Host "   路径: $npmPrefix" -ForegroundColor Cyan
    
    $globalModules = Join-Path $npmPrefix "node_modules"
    if (Test-Path $globalModules) {
        Write-Host "   ✓ 全局 node_modules 存在" -ForegroundColor Green
        
        # 检查 Stagehand
        $stagehandPath = Join-Path $globalModules "@browserbasehq\stagehand"
        if (Test-Path $stagehandPath) {
            Write-Host "   ✓ Stagehand 已安装" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Stagehand 未安装" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "   ✗ 无法获取 npm 配置" -ForegroundColor Red
}

# 检查 Playwright
Write-Host "`n5. 检查 Playwright:" -ForegroundColor Yellow
$playwrightPath = Join-Path $env:LOCALAPPDATA "ms-playwright"
if (Test-Path $playwrightPath) {
    Write-Host "   ✓ Playwright 已安装" -ForegroundColor Green
    Write-Host "   路径: $playwrightPath" -ForegroundColor Cyan
    
    $browsers = Get-ChildItem $playwrightPath -Directory
    Write-Host "   浏览器: $($browsers.Count) 个" -ForegroundColor Cyan
    foreach ($browser in $browsers) {
        Write-Host "     - $($browser.Name)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ✗ Playwright 未安装" -ForegroundColor Yellow
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green
