namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.IntegrationEvents;

public interface IOrderingIntegrationEventService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transactionId">事务标识符</param>
    /// <returns></returns>
    Task PublishEventsThroughEventBusAsync(Guid transactionId);
    Task AddAndSaveEventAsync(IntegrationEvent evt);
}
