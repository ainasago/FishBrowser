@echo off
echo 修复语法错误...

REM 修复FingerprintApplicationService.cs中的字符串错误
powershell -Command "(Get-Content 'Application\Services\FingerprintApplicationService.cs' -Raw) -replace 'throw new FileNotFoundException\(\"文件不存�?, filePath\)', 'throw new FileNotFoundException(\"文件不存在\", filePath)' | Set-Content 'Application\Services\FingerprintApplicationService.cs' -Encoding UTF8"

powershell -Command "(Get-Content 'Application\Services\FingerprintApplicationService.cs' -Raw) -replace '/// 导入所有预设指�?', '/// 导入所有预设指纹' | Set-Content 'Application\Services\FingerprintApplicationService.cs' -Encoding UTF8"

powershell -Command "(Get-Content 'Application\Services\FingerprintApplicationService.cs' -Raw) -replace '/// 获取所有预设指�?', '/// 获取所有预设指纹' | Set-Content 'Application\Services\FingerprintApplicationService.cs' -Encoding UTF8"

powershell -Command "(Get-Content 'Application\Services\FingerprintApplicationService.cs' -Raw) -replace '/// �?JSON 文件导入指纹配置（存在同名则跳过或更新名称冲突逻辑�?', '/// 从JSON文件导入指纹配置（存在同名则跳过或更新名称冲突逻辑）' | Set-Content 'Application\Services\FingerprintApplicationService.cs' -Encoding UTF8"

powershell -Command "(Get-Content 'Application\Services\FingerprintApplicationService.cs' -Raw) -replace '/// 获取所有指纹配�?', '/// 获取所有指纹配置' | Set-Content 'Application\Services\FingerprintApplicationService.cs' -Encoding UTF8"

echo 修复完成
pause