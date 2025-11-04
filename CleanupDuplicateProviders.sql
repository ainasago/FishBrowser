-- 检查重复的 Provider 数据
SELECT Name, ProviderType, ModelId, COUNT(*) as Count
FROM AIProviderConfigs
GROUP BY Name, ProviderType, ModelId
HAVING COUNT(*) > 1;

-- 查看所有 Provider
SELECT Id, Name, ProviderType, ModelId, BaseUrl, CreatedAt
FROM AIProviderConfigs
ORDER BY CreatedAt DESC;

-- 删除重复的 Provider（保留最新的）
-- 注意：运行前请备份数据库！
/*
DELETE FROM AIProviderConfigs
WHERE Id NOT IN (
    SELECT MAX(Id)
    FROM AIProviderConfigs
    GROUP BY Name, ProviderType, ModelId
);
*/

-- 清理孤立的 API Keys（没有对应 Provider 的）
/*
DELETE FROM AIApiKeys
WHERE ProviderId NOT IN (SELECT Id FROM AIProviderConfigs);
*/

-- 清理孤立的 Settings
/*
DELETE FROM AIProviderSettings
WHERE ProviderId NOT IN (SELECT Id FROM AIProviderConfigs);
*/
