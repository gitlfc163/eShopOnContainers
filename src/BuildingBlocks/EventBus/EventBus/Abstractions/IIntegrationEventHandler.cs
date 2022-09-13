namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// 事件处理的接口
/// 通过强类型的IntegrationEvent参数约束
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// 事件处理
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
}
