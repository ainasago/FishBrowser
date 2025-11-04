# 配置 npm 镜像源（淘宝镜像）

Write-Host "========================================" -ForegroundColor Green
Write-Host "配置 npm 镜像源（淘宝镜像）" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "[1/4] 当前 npm 配置：" -ForegroundColor Yellow
$currentRegistry = npm config get registry
Write-Host "   $currentRegistry" -ForegroundColor Cyan
Write-Host ""

Write-Host "[2/4] 设置淘宝镜像源..." -ForegroundColor Yellow
npm config set registry https://registry.npmmirror.com
Write-Host "   ✓ 已设置为淘宝镜像" -ForegroundColor Green
Write-Host ""

Write-Host "[3/4] 验证新配置：" -ForegroundColor Yellow
$newRegistry = npm config get registry
Write-Host "   $newRegistry" -ForegroundColor Cyan
Write-Host ""

Write-Host "[4/4] 测试连接速度..." -ForegroundColor Yellow
$testStart = Get-Date
try {
    $info = npm info @browserbasehq/stagehand --json 2>&1 | ConvertFrom-Json
    $testEnd = Get-Date
    $duration = ($testEnd - $testStart).TotalSeconds
    
    Write-Host "   ✓ 连接成功！" -ForegroundColor Green
    Write-Host "   响应时间: $([math]::Round($duration, 2)) 秒" -ForegroundColor Cyan
    Write-Host "   最新版本: $($info.version)" -ForegroundColor Cyan
} catch {
    Write-Host "   ⚠ 测试失败，但镜像已配置" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "配置完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "现在可以重新尝试安装 Stagehand" -ForegroundColor Cyan
Write-Host ""
Write-Host "如需恢复默认镜像，运行：" -ForegroundColor Gray
Write-Host "npm config set registry https://registry.npmjs.org" -ForegroundColor Gray
Write-Host ""
