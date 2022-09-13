namespace Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents.Events;

/// <summary>
/// 价格发生变化好布事件-集成事件说明：
/// 事件是“过去发生的事情”，因此它的名称必须是过去时。
/// 集成事件是一个可能对其他微服务、限界上下文或外部系统产生副作用的事件。
/// </summary>
public record ProductPriceChangedIntegrationEvent : IntegrationEvent
{
    public int ProductId { get; private init; }

    public decimal NewPrice { get; private init; }

    public decimal OldPrice { get; private init; }

    /// <summary>
    /// 如果价格发生变化，保存产品数据并通过事件总线发布集成事件
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="newPrice"></param>
    /// <param name="oldPrice"></param>
    public ProductPriceChangedIntegrationEvent(int productId, decimal newPrice, decimal oldPrice)
    {
        ProductId = productId;
        NewPrice = newPrice;
        OldPrice = oldPrice;
    }
}
