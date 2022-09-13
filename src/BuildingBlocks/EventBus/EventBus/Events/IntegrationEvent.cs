namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;

/// <summary>
/// 事件源基类(集成事件)
/// 集成事件:集成事件可用于跨多个微服务或外部系统同步领域状态，这是通过在微服务之外发布集成事件来实现的
/// 具体的事件可以通过继承该类，来完善事件的描述信息
/// </summary>
public record IntegrationEvent
{        
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonConstructor]
    public IntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }
    /// <summary>
    /// Guid
    /// </summary>
    [JsonInclude]
    public Guid Id { get; private init; }
    /// <summary>
    /// 创建日期
    /// </summary>
    [JsonInclude]
    public DateTime CreationDate { get; private init; }
}
