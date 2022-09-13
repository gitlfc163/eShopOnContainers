namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBusServiceBus;

/// <summary>
/// �����������ӽӿ�
/// </summary>
public interface IServiceBusPersisterConnection : IAsyncDisposable
{
    ServiceBusClient TopicClient { get; }
    ServiceBusAdministrationClient AdministrationClient { get; }
}