namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBusServiceBus;

/// <summary>
/// 服务总线连接接口
/// </summary>
public interface IServiceBusPersisterConnection : IAsyncDisposable
{
    ServiceBusClient TopicClient { get; }
    ServiceBusAdministrationClient AdministrationClient { get; }
}