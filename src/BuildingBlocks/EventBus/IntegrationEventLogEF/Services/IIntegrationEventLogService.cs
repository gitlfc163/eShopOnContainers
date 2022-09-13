namespace Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF.Services;

/// <summary>
/// 暴露IIntegrationEventLogService用于事件状态的更新
/// </summary>
public interface IIntegrationEventLogService
{
    /// <summary>
    /// 异步检索等待发布的事件日志
    /// </summary>
    /// <param name="transactionId">事务标识符</param>
    /// <returns></returns>
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    /// <summary>
    /// 异步保存事件日志
    /// </summary>
    /// <param name="event"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction);

    /// <summary>
    /// 异步将事件标记为已发布
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task MarkEventAsPublishedAsync(Guid eventId);


    /// <summary>
    /// 异步将事件标记为发布中
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task MarkEventAsInProgressAsync(Guid eventId);

    /// <summary>
    /// 异步将事件标记为发布失败
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task MarkEventAsFailedAsync(Guid eventId);
}
