namespace Microsoft.eShopOnContainers.Services.Basket.API.Model;

/// <summary>
///  ʵ��
/// </summary>
public class CustomerBasket
{
    /// <summary>
    /// ��� ID���ͻ�ID��
    /// </summary>
    public string BuyerId { get; set; }

    /// <summary>
    /// ���ﳵ������
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

