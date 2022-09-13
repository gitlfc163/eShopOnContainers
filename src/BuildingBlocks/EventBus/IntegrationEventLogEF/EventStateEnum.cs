namespace Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF;

/// <summary>
/// 事件状态枚举
/// </summary>
public enum EventStateEnum
{
    /// <summary>
    /// 未发布
    /// </summary>
    NotPublished = 0,

    //发布中
    InProgress = 1,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 2,

    /// <summary>
    /// 发布失败
    /// </summary>
    PublishedFailed = 3
}

