using System;

namespace FishBrowser.WPF.Domain.Entities;

/// <summary>
/// 领域实体基类
/// 所有领域实体都应该继承此类
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// 实体 ID
    /// </summary>
    public int Id { get; protected set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// 检查两个实体是否相等（基于 ID）
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id == 0 || other.Id == 0)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// 比较操作符
    /// </summary>
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// 不相等操作符
    /// </summary>
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
