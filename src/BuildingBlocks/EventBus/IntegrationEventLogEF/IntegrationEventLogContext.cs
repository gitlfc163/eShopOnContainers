namespace Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF;

/// <summary>
/// EF上下文用于事件日志持久化
/// 其他微服务通过在启动类中注册IntegrationEventLogContext即可完成事件日志的集成
/// </summary>
public class IntegrationEventLogContext : DbContext
{
    public IntegrationEventLogContext(DbContextOptions<IntegrationEventLogContext> options) : base(options)
    {
    }

    public DbSet<IntegrationEventLogEntry> IntegrationEventLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<IntegrationEventLogEntry>(ConfigureIntegrationEventLogEntry);
    }

    void ConfigureIntegrationEventLogEntry(EntityTypeBuilder<IntegrationEventLogEntry> builder)
    {
        builder.ToTable("IntegrationEventLog");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.Content)
            .IsRequired();

        builder.Property(e => e.CreationTime)
            .IsRequired();

        builder.Property(e => e.State)
            .IsRequired();

        builder.Property(e => e.TimesSent)
            .IsRequired();

        builder.Property(e => e.EventTypeName)
            .IsRequired();

    }
}
