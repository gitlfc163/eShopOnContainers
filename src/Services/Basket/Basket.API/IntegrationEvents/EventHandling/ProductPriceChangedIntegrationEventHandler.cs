namespace Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.EventHandling;

/// <summary>
/// 产品价格改变集成事件处理程序
/// 修改所有购物车
/// </summary>
public class ProductPriceChangedIntegrationEventHandler : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    private readonly ILogger<ProductPriceChangedIntegrationEventHandler> _logger;
    private readonly IBasketRepository _repository;

    public ProductPriceChangedIntegrationEventHandler(
        ILogger<ProductPriceChangedIntegrationEventHandler> logger,
        IBasketRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task Handle(ProductPriceChangedIntegrationEvent @event)
    {
        using (LogContext.PushProperty("IntegrationEventContext", $"{@event.Id}-{Program.AppName}"))
        {
            _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, Program.AppName, @event);

            var userIds = _repository.GetUsers();

            // 获取所有用户的购物车
            foreach (var id in userIds)
            {
                var basket = await _repository.GetBasketAsync(id);

                await UpdatePriceInBasketItems(@event.ProductId, @event.NewPrice, @event.OldPrice, basket);
            }
        }
    }
    /// <summary>
    /// 更新购物车中的产品价格
    /// 在商品价格发生变化时
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="newPrice"></param>
    /// <param name="oldPrice"></param>
    /// <param name="basket"></param>
    /// <returns></returns>
    private async Task UpdatePriceInBasketItems(int productId, decimal newPrice, decimal oldPrice, CustomerBasket basket)
    {
        var itemsToUpdate = basket?.Items?.Where(x => x.ProductId == productId).ToList();

        if (itemsToUpdate != null)
        {
            _logger.LogInformation("----- ProductPriceChangedIntegrationEventHandler - Updating items in basket for user: {BuyerId} ({@Items})", basket.BuyerId, itemsToUpdate);

            foreach (var item in itemsToUpdate)
            {
                if (item.UnitPrice == oldPrice)
                {
                    var originalPrice = item.UnitPrice;
                    item.UnitPrice = newPrice;
                    item.OldUnitPrice = originalPrice;
                }
            }
            await _repository.UpdateBasketAsync(basket);
        }
    }
}
