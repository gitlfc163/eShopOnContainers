namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// 事件总线接口
/// </summary>
public interface IEventBus
{
    void Publish(IntegrationEvent @event);

    /// <summary>
    /// 事件订阅-强约束方式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// 事件订阅-动态类型方式
    /// </summary>
    /// <typeparam name="TH"></typeparam>
    /// <param name="eventName"></param>
    void SubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    /// <summary>
    /// 取消事件订阅-动态类型方式
    /// </summary>
    /// <typeparam name="TH"></typeparam>
    /// <param name="eventName"></param>
    void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    /// <summary>
    ///  取消事件订阅-强约束方式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent;
}
