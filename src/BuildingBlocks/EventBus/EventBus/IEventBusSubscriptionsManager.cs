namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBus;

/// <summary>
/// 事件订阅管理
/// 用于维护事件的订阅和注销，以及订阅信息的持久化
/// 其默认的实现InMemoryEventBusSubscriptionsManager就是使用内存进行存储事件源和事件处理的映射字典
/// </summary>
public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }

    event EventHandler<string> OnEventRemoved;

    void AddDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    void AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;
    void RemoveDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;
    bool HasSubscriptionsForEvent(string eventName);
    Type GetEventTypeByName(string eventName);
    void Clear();
    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
    string GetEventKey<T>();
}
