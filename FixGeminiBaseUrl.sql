-- 修复 Gemini 配置的 Base URL
-- 问题：Base URL 被错误设置为包含 /chat 的路径，导致使用了 OpenAI 格式

-- 1. 查看当前所有 Gemini 配置
SELECT Id, Name, ProviderType, BaseUrl, ModelId
FROM AIProviderConfigs
WHERE ProviderType = 2;  -- GoogleGemini = 2

-- 2. 修复 Base URL（移除 /chat 或其他错误路径）
UPDATE AIProviderConfigs
SET BaseUrl = 'https://generativelanguage.googleapis.com/v1beta'
WHERE ProviderType = 2 
  AND (BaseUrl LIKE '%/chat%' OR BaseUrl != 'https://generativelanguage.googleapis.com/v1beta');

-- 3. 验证修复结果
SELECT Id, Name, ProviderType, BaseUrl, ModelId
FROM AIProviderConfigs
WHERE ProviderType = 2;

-- 4. 同时更新模型 ID 为最新版本（可选）
/*
UPDATE AIProviderConfigs
SET ModelId = 'gemini-2.0-flash-exp'
WHERE ProviderType = 2 AND ModelId = 'gemini-pro';
*/
