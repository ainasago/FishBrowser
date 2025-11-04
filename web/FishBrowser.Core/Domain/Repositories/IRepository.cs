using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishBrowser.WPF.Domain.Entities;
using Entity = FishBrowser.WPF.Domain.Entities.Entity;

namespace FishBrowser.WPF.Domain.Repositories;

/// <summary>
/// 仓储基础接口
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// 按 ID 获取实体
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 添加实体
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task DeleteAsync(T entity);

    /// <summary>
    /// 删除实体（按 ID）
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 保存更改
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id);
}
