namespace Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents;

public class CatalogIntegrationEventService : ICatalogIntegrationEventService, IDisposable
{
    private readonly Func<DbConnection, IIntegrationEventLogService> _integrationEventLogServiceFactory;
    private readonly IEventBus _eventBus;
    private readonly CatalogContext _catalogContext;
    private readonly IIntegrationEventLogService _eventLogService;
    private readonly ILogger<CatalogIntegrationEventService> _logger;
    private volatile bool disposedValue;

    public CatalogIntegrationEventService(
        ILogger<CatalogIntegrationEventService> logger,
        IEventBus eventBus,
        CatalogContext catalogContext,
        Func<DbConnection, IIntegrationEventLogService> integrationEventLogServiceFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _catalogContext = catalogContext ?? throw new ArgumentNullException(nameof(catalogContext));
        _integrationEventLogServiceFactory = integrationEventLogServiceFactory ?? throw new ArgumentNullException(nameof(integrationEventLogServiceFactory));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _eventLogService = _integrationEventLogServiceFactory(_catalogContext.Database.GetDbConnection());
    }

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            _logger.LogInformation("----- Publishing integration event: {IntegrationEventId_published} from {AppName} - ({@IntegrationEvent})", evt.Id, Program.AppName, evt);

            await _eventLogService.MarkEventAsInProgressAsync(evt.Id);
            //发布事件
            _eventBus.Publish(evt);
            await _eventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR Publishing integration event: {IntegrationEventId} from {AppName} - ({@IntegrationEvent})", evt.Id, Program.AppName, evt);
            await _eventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        _logger.LogInformation("----- CatalogIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}", evt.Id);

        //在显式 BeginTransaction() 中使用多个 DbContext 时使用 EF Core 弹性策略:
        //See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency            
        await ResilientTransaction.New(_catalogContext).ExecuteAsync(async () =>
        {
            // 由于本地事务，在原始目录数据库操作和 IntegrationEventLog 之间实现原子性
            await _catalogContext.SaveChangesAsync();
            await _eventLogService.SaveEventAsync(evt, _catalogContext.Database.CurrentTransaction);
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                (_eventLogService as IDisposable)?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
