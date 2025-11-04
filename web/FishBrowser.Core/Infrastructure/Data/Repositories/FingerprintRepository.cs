using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Domain.Repositories;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Infrastructure.Data.Repositories;

/// <summary>
/// 指纹配置仓储实现
/// </summary>
public class FingerprintRepository : IFingerprintRepository
{
    private readonly WebScraperDbContext _dbContext;

    public FingerprintRepository(WebScraperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 按 ID 获取指纹配置
    /// </summary>
    public async Task<FingerprintProfile?> GetByIdAsync(int id)
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Id == id)
        );
    }

    /// <summary>
    /// 获取所有指纹配置
    /// </summary>
    public async Task<IEnumerable<FingerprintProfile>> GetAllAsync()
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.ToList()
        );
    }

    /// <summary>
    /// 按名称获取指纹配置
    /// </summary>
    public async Task<FingerprintProfile?> GetByNameAsync(string name)
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Name == name)
        );
    }

    /// <summary>
    /// 获取所有预设指纹
    /// </summary>
    public async Task<IEnumerable<FingerprintProfile>> GetPresetsAsync()
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.Where(f => f.IsPreset).ToList()
        );
    }

    /// <summary>
    /// 检查名称是否已存在
    /// </summary>
    public async Task<bool> NameExistsAsync(string name)
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.Any(f => f.Name == name)
        );
    }

    /// <summary>
    /// 添加指纹配置
    /// </summary>
    public async Task AddAsync(FingerprintProfile entity)
    {
        _dbContext.FingerprintProfiles.Add(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 更新指纹配置
    /// </summary>
    public async Task UpdateAsync(FingerprintProfile entity)
    {
        var existing = _dbContext.FingerprintProfiles.FirstOrDefault(f => f.Id == entity.Id);
        if (existing != null)
        {
            _dbContext.FingerprintProfiles.Remove(existing);
            _dbContext.FingerprintProfiles.Add(entity);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 删除指纹配置
    /// </summary>
    public async Task DeleteAsync(FingerprintProfile entity)
    {
        _dbContext.FingerprintProfiles.Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 删除指纹配置（按 ID）
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
            _dbContext.FingerprintProfiles.Remove(entity);
    }

    /// <summary>
    /// 按名称删除指纹配置
    /// </summary>
    public async Task DeleteByNameAsync(string name)
    {
        var entity = await GetByNameAsync(name);
        if (entity != null)
            _dbContext.FingerprintProfiles.Remove(entity);
    }

    /// <summary>
    /// 检查指纹配置是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(int id)
    {
        return await Task.FromResult(
            _dbContext.FingerprintProfiles.Any(f => f.Id == id)
        );
    }

    /// <summary>
    /// 保存更改
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await Task.FromResult(_dbContext.SaveChanges());
    }
}
