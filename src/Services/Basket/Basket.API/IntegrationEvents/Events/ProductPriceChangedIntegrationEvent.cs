﻿namespace Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.Events;

/// <summary>
/// 商品价格修改事件
/// </summary>
// 集成事件记录:
// 一个事件是“过去发生的事情”，因此它的名字必须是
// 集成事件是可能对其他微服务、受限上下文或外部系统造成副作用的事件。
public record ProductPriceChangedIntegrationEvent : IntegrationEvent
{
    public int ProductId { get; private init; }

    public decimal NewPrice { get; private init; }

    public decimal OldPrice { get; private init; }

    public ProductPriceChangedIntegrationEvent(int productId, decimal newPrice, decimal oldPrice)
    {
        ProductId = productId;
        NewPrice = newPrice;
        OldPrice = oldPrice;
    }
}
