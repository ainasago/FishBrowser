# 修复字符串替换导致的语法错误
# 这个脚本将修复PowerShell替换命名空间时导致的字符串错误

Get-ChildItem -Path "." -Recurse -Include "*.cs" | ForEach-Object {
    $file = $_
    $content = Get-Content -Path $file.FullName -Raw
    
    # 修复字符串中的错误替换
    $content = $content -replace 'throw new FileNotFoundException\("文件不存�?, filePath\)', 'throw new FileNotFoundException("文件不存在", filePath)'
    $content = $content -replace '/// 导入所有预设指�?', '/// 导入所有预设指纹'
    $content = $content -replace '/// 获取所有预设指�?', '/// 获取所有预设指纹'
    $content = $content -replace '/// �?JSON 文件导入指纹配置（存在同名则跳过或更新名称冲突逻辑�?', '/// 从JSON文件导入指纹配置（存在同名则跳过或更新名称冲突逻辑）'
    $content = $content -replace '/// 获取所有指纹配�?', '/// 获取所有指纹配置'
    
    # 修复其他可能的字符串错误
    $content = $content -replace 'namespace FishBrowser\.Core\.Infrastructure\.Data', 'namespace FishBrowser.Core.Infrastructure.Data'
    $content = $content -replace 'namespace FishBrowser\.Core\.Domain\.Services', 'namespace FishBrowser.Core.Domain.Services'
    $content = $content -replace 'namespace FishBrowser\.Core\.Engine', 'namespace FishBrowser.Core.Engine'
    $content = $content -replace 'namespace FishBrowser\.Core\.Data', 'namespace FishBrowser.Core.Data'
    
    # 修复其他可能的错误
    $content = $content -replace 'FishBrowser\.Core\.Infrastructure\.Data', 'FishBrowser.Core.Infrastructure.Data'
    $content = $content -replace 'FishBrowser\.Core\.Domain\.Services', 'FishBrowser.Core.Domain.Services'
    $content = $content -replace 'FishBrowser\.Core\.Engine', 'FishBrowser.Core.Engine'
    $content = $content -replace 'FishBrowser\.Core\.Data', 'FishBrowser.Core.Data'
    
    Set-Content -Path $file.FullName -Value $content -Encoding UTF8
}

Write-Host "语法错误修复完成"