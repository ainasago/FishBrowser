# 批量更新using语句脚本
# 此脚本将把所有FishBrowser.WPF的using语句替换为FishBrowser.Core

$rootPath = "d:\1Dev\webbrowser\web\FishBrowser.Core"

# 获取所有.cs文件
$files = Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # 替换各种using语句
    $content = $content -replace 'using FishBrowser.WPF\.Models', 'using FishBrowser.Core.Models'
    $content = $content -replace 'using FishBrowser.WPF\.Services', 'using FishBrowser.Core.Services'
    $content = $content -replace 'using FishBrowser.WPF\.Engine', 'using FishBrowser.Core.Engine'
    $content = $content -replace 'using FishBrowser.WPF\.Domain\.Repositories', 'using FishBrowser.Core.Domain.Repositories'
    $content = $content -replace 'using FishBrowser.WPF\.Domain\.Services', 'using FishBrowser.Core.Domain.Services'
    $content = $content -replace 'using FishBrowser.WPF\.Domain\.Entities', 'using FishBrowser.Core.Domain.Entities'
    $content = $content -replace 'using FishBrowser.WPF\.Domain\.ValueObjects', 'using FishBrowser.Core.Domain.ValueObjects'
    $content = $content -replace 'using FishBrowser.WPF\.Data', 'using FishBrowser.Core.Data'
    $content = $content -replace 'using FishBrowser.WPF\.Infrastructure\.Data', 'using FishBrowser.Core.Infrastructure.Data'
    $content = $content -replace 'using FishBrowser.WPF\.Infrastructure\.Configuration', 'using FishBrowser.Core.Infrastructure.Configuration'
    $content = $content -replace 'using FishBrowser.WPF\.Application\.DTOs', 'using FishBrowser.Core.Application.DTOs'
    $content = $content -replace 'using FishBrowser.WPF\.Application\.Services', 'using FishBrowser.Core.Application.Services'
    $content = $content -replace 'using FishBrowser.WPF\.Application\.Mappers', 'using FishBrowser.Core.Application.Mappers'
    $content = $content -replace 'using FishBrowser.WPF\.Presentation\.ViewModels', 'using FishBrowser.Core.Presentation.ViewModels'
    $content = $content -replace 'using FishBrowser.WPF\.Presentation\.Commands', 'using FishBrowser.Core.Presentation.Commands'
    $content = $content -replace 'using FishBrowser.WPF\.ViewModels', 'using FishBrowser.Core.ViewModels'
    $content = $content -replace 'using FishBrowser.WPF\.Services\.AIProviderAdapters', 'using FishBrowser.Core.Services.AIProviderAdapters'
    
    # 保存文件
    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host "using语句更新完成"