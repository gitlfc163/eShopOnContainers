namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBusRabbitMQ;

/// <summary>
/// 连接到对应的Broke
/// </summary>
public interface IRabbitMQPersistentConnection
    : IDisposable
{
    bool IsConnected { get; }

    /// <summary>
    /// 建立连接
    /// </summary>
    /// <returns></returns>
    bool TryConnect();

    /// <summary>
    /// 创建并返回一个新的通道、会话和模型
    /// </summary>
    /// <returns></returns>
    IModel CreateModel();
}
