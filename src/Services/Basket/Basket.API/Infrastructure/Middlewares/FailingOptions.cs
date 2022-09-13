namespace Basket.API.Infrastructure.Middlewares;

/// <summary>
/// 中断中间件
/// 通过访问http://localhost:5103/failing获取该中间件的启用状态，通过请求参数指定：
/// 即通过http://localhost:5103/failing?enable和http://localhost:5103/failing?disable 
/// 来手动中断和恢复服务，来模拟断路，以便用于测试断路器模式。
/// 开启断路后，当访问购物车页面时，Polly在重试指定次数依然无法访问服务时，就会抛出BrokenCircuitException异常，
/// 通过捕捉该异常告知用户稍后再试。
/// </summary>
public class FailingOptions
{
    public string ConfigPath = "/Failing";
    public List<string> EndpointPaths { get; set; } = new List<string>();

    public List<string> NotFilteredPaths { get; set; } = new List<string>();
}

