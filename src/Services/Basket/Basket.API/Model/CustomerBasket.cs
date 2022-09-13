namespace Microsoft.eShopOnContainers.Services.Basket.API.Model;

/// <summary>
///  实体
/// </summary>
public class CustomerBasket
{
    /// <summary>
    /// 买家 ID（客户ID）
    /// </summary>
    public string BuyerId { get; set; }

    /// <summary>
    /// 购物车详情项
    /// </summary>
    public List<BasketItem> Items { get; set; } = new();

    public CustomerBasket()
    {

    }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}

