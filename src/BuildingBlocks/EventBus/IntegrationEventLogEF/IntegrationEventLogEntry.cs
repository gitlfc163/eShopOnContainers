namespace Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF;

/// <summary>
/// 集成事件实体
/// </summary>
public class IntegrationEventLogEntry
{
    private IntegrationEventLogEntry() { }
    public IntegrationEventLogEntry(IntegrationEvent @event, Guid transactionId)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationDate;
        EventTypeName = @event.GetType().FullName;                     
        Content = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId.ToString();
    }
    /// <summary>
    /// 事件Id
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    public string EventTypeName { get; private set; }

    /// <summary>
    /// 事件类型简称
    /// </summary>
    [NotMapped]
    public string EventTypeShortName => EventTypeName.Split('.')?.Last();

    /// <summary>
    /// 事件源基类
    /// </summary>
    [NotMapped]
    public IntegrationEvent IntegrationEvent { get; private set; }

    /// <summary>
    /// 事件状态
    /// </summary>
    public EventStateEnum State { get; set; }

    /// <summary>
    /// 发送次数
    /// </summary>
    public int TimesSent { get; set; }

    /// <summary>
    /// 事件发生的时间
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// 事务标识符
    /// </summary>
    public string TransactionId { get; private set; }

    public IntegrationEventLogEntry DeserializeJsonContent(Type type)
    {            
        IntegrationEvent = JsonSerializer.Deserialize(Content, type, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) as IntegrationEvent;
        return this;
    }
}
