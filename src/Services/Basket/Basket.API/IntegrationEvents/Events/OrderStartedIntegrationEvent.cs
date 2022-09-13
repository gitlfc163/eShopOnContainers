namespace Basket.API.IntegrationEvents.Events;

/// <summary>
/// 订单启动事件
/// </summary>
public record OrderStartedIntegrationEvent : IntegrationEvent
{
    public string UserId { get; init; }

    public OrderStartedIntegrationEvent(string userId)
        => UserId = userId;
}
