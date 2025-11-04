-- 修复 Provider Type 错误
-- 问题：数据库中保存的 ProviderType = 2，但枚举中 2 = AzureOpenAI，3 = GoogleGemini

-- 1. 查看当前所有配置的 ProviderType
SELECT Id, Name, ProviderType, BaseUrl, ModelId
FROM AIProviderConfigs
ORDER BY ProviderType;

-- 2. 修复 Gemini 配置（将 ProviderType 从 2 改为 3）
UPDATE AIProviderConfigs
SET ProviderType = 3
WHERE BaseUrl LIKE '%generativelanguage.googleapis.com%'
   OR ModelId LIKE 'gemini%';

-- 3. 验证修复结果
SELECT Id, Name, ProviderType, BaseUrl, ModelId
FROM AIProviderConfigs
WHERE ProviderType = 3;

-- 4. 检查是否还有其他错误的配置
SELECT 
    Id, 
    Name, 
    ProviderType,
    CASE ProviderType
        WHEN 1 THEN 'OpenAI'
        WHEN 2 THEN 'AzureOpenAI'
        WHEN 3 THEN 'GoogleGemini'
        WHEN 4 THEN 'AnthropicClaude'
        WHEN 101 THEN 'AlibabaQwen'
        WHEN 109 THEN 'ModelScope'
        WHEN 110 THEN 'SiliconFlow'
        WHEN 201 THEN 'Ollama'
        ELSE 'Unknown'
    END AS ProviderTypeName,
    BaseUrl,
    ModelId
FROM AIProviderConfigs
ORDER BY ProviderType;
