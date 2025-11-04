# WebScraper Playwright 安装脚本
# 作者: WebScraper Team
# 版本: 1.0

param(
    [switch]$Force,
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   WebScraper Playwright 安装脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查管理员权限
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 显示进度条
function Show-Progress {
    param(
        [string]$Activity,
        [string]$Status,
        [int]$PercentComplete
    )
    Write-Progress -Activity $Activity -Status $Status -PercentComplete $PercentComplete
}

try {
    # 检查管理员权限
    if (-not (Test-Administrator)) {
        Write-Warning "建议以管理员身份运行此脚本以获得最佳体验"
        Write-Host "继续安装..." -ForegroundColor Yellow
        Write-Host ""
    }

    # 检查网络连接
    Write-Host "检查网络连接..." -ForegroundColor Blue
    try {
        Test-NetConnection -ComputerName "google.com" -Port 443 -InformationLevel Quiet | Out-Null
        Write-Host "✓ 网络连接正常" -ForegroundColor Green
    }
    catch {
        Write-Warning "网络连接可能有问题，但继续尝试安装"
    }
    Write-Host ""

    # 步骤 1: 安装 Playwright CLI 工具
    Write-Host "步骤 1/3: 安装 Playwright CLI 工具" -ForegroundColor Blue
    Show-Progress -Activity "安装 Playwright" -Status "安装 CLI 工具..." -PercentComplete 10
    
    try {
        $result = dotnet tool install --global Microsoft.Playwright.CLI
        if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1) {  # 1 = already installed
            Write-Host "✓ Playwright CLI 工具安装成功" -ForegroundColor Green
        } else {
            Write-Warning "Playwright CLI 工具安装可能有问题，但继续..."
        }
    }
    catch {
        Write-Warning "Playwright CLI 工具安装失败: $_"
        Write-Host "继续尝试安装浏览器..." -ForegroundColor Yellow
    }
    Write-Host ""

    # 步骤 2: 安装浏览器
    Write-Host "步骤 2/3: 下载并安装浏览器" -ForegroundColor Blue
    Show-Progress -Activity "安装 Playwright" -Status "下载浏览器..." -PercentComplete 30
    
    try {
        # 检查 playwright 命令是否可用
        $playwrightVersion = playwright --version 2>$null
        if ($playwrightVersion) {
            Write-Host "✓ Playwright CLI 可用: $playwrightVersion" -ForegroundColor Green
        } else {
            Write-Warning "Playwright CLI 不可用，尝试其他方法..."
        }
        
        Show-Progress -Activity "安装 Playwright" -Status "安装浏览器..." -PercentComplete 50
        
        # 执行安装
        $installResult = playwright install 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ 浏览器安装成功" -ForegroundColor Green
        } else {
            Write-Error "浏览器安装失败"
            Write-Host "错误信息:" -ForegroundColor Red
            $installResult | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
            throw "Playwright 安装失败"
        }
    }
    catch {
        Write-Error "浏览器安装过程中出错: $_"
        Write-Host ""
        Write-Host "尝试备用安装方法..." -ForegroundColor Yellow
        
        # 备用方法: 使用项目脚本
        $projectDir = Get-Location
        $binDir = Join-Path $projectDir "bin\Debug\net9.0-windows"
        $playwrightScript = Join-Path $binDir "playwright.ps1"
        
        if (Test-Path $playwrightScript) {
            Write-Host "使用项目脚本安装..." -ForegroundColor Blue
            & pwsh -File $playwrightScript install
        } else {
            throw "找不到项目安装脚本，请手动安装"
        }
    }
    Write-Host ""

    # 步骤 3: 验证安装
    Write-Host "步骤 3/3: 验证安装" -ForegroundColor Blue
    Show-Progress -Activity "安装 Playwright" -Status "验证安装..." -PercentComplete 90
    
    $chromiumPath = Join-Path $env:LOCALAPPDATA "ms-playwright\chromium-1091\chrome-win\chrome.exe"
    if (Test-Path $chromiumPath) {
        Write-Host "✓ Chromium 浏览器安装成功" -ForegroundColor Green
        Write-Host "  位置: $chromiumPath" -ForegroundColor Gray
    } else {
        Write-Warning "Chromium 浏览器未找到，但安装可能已完成"
    }
    
    Show-Progress -Activity "安装 Playwright" -Status "完成" -PercentComplete 100
    Write-Host ""

    # 完成
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   安装完成！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步操作:" -ForegroundColor Cyan
    Write-Host "1. 重新启动 WebScraper 应用" -ForegroundColor White
    Write-Host "2. 点击"运行测试"按钮验证安装" -ForegroundColor White
    Write-Host "3. 如果仍有问题，请查看帮助文档" -ForegroundColor White
    Write-Host ""
    Write-Host "安装位置: $env:LOCALAPPDATA\ms-playwright" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "   安装失败！" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "错误信息: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "手动安装步骤:" -ForegroundColor Yellow
    Write-Host "1. 打开 PowerShell (以管理员身份)" -ForegroundColor White
    Write-Host "2. 运行: dotnet tool install --global Microsoft.Playwright.CLI" -ForegroundColor White
    Write-Host "3. 运行: playwright install" -ForegroundColor White
    Write-Host ""
    Write-Host "如果仍然失败，请联系技术支持。" -ForegroundColor Yellow
    exit 1
}

# 询问是否打开应用
$openApp = Read-Host "是否现在打开 WebScraper 应用? (y/N)"
if ($openApp -eq 'y' -or $openApp -eq 'Y') {
    Write-Host "启动应用..." -ForegroundColor Blue
    $exePath = Join-Path (Get-Location) "bin\Debug\net9.0-windows\WebScraperApp.exe"
    if (Test-Path $exePath) {
        Start-Process $exePath
    } else {
        Write-Warning "找不到应用文件，请手动启动"
    }
}

Write-Host "脚本执行完成。" -ForegroundColor Green
