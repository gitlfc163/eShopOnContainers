namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// 事件处理的接口
/// 通过dynamic参数约束可以简化事件源的构建，更趋于灵活
/// </summary>
public interface IDynamicIntegrationEventHandler
{
    /// <summary>
    /// 事件处理
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    Task Handle(dynamic eventData);
}
