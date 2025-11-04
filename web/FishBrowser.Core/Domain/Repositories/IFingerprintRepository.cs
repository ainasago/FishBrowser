using System.Collections.Generic;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Domain.Repositories;

/// <summary>
/// 指纹配置仓储接口
/// </summary>
public interface IFingerprintRepository
{
    /// <summary>
    /// 按 ID 获取指纹配置
    /// </summary>
    Task<FingerprintProfile?> GetByIdAsync(int id);

    /// <summary>
    /// 获取所有指纹配置
    /// </summary>
    Task<IEnumerable<FingerprintProfile>> GetAllAsync();

    /// <summary>
    /// 按名称获取指纹配置
    /// </summary>
    Task<FingerprintProfile?> GetByNameAsync(string name);

    /// <summary>
    /// 获取所有预设指纹
    /// </summary>
    Task<IEnumerable<FingerprintProfile>> GetPresetsAsync();

    /// <summary>
    /// 检查名称是否已存在
    /// </summary>
    Task<bool> NameExistsAsync(string name);

    /// <summary>
    /// 添加指纹配置
    /// </summary>
    Task AddAsync(FingerprintProfile entity);

    /// <summary>
    /// 更新指纹配置
    /// </summary>
    Task UpdateAsync(FingerprintProfile entity);

    /// <summary>
    /// 删除指纹配置
    /// </summary>
    Task DeleteAsync(FingerprintProfile entity);

    /// <summary>
    /// 按 ID 删除指纹配置
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 按名称删除指纹配置
    /// </summary>
    Task DeleteByNameAsync(string name);

    /// <summary>
    /// 保存更改
    /// </summary>
    Task SaveChangesAsync();
}
