using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Domain.Entities;

/// <summary>
/// 聚合根基类
/// 聚合根是领域驱动设计中的概念，用于维护聚合内的一致性
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// 获取所有领域事件
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 添加领域事件
    /// </summary>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 清除所有领域事件
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// 领域事件基类
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// 事件是否已发布
    /// </summary>
    public bool IsPublished { get; set; }
}
