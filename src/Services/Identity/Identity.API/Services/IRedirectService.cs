namespace Microsoft.eShopOnContainers.Services.Identity.API.Services
{
    /// <summary>
    /// 跳转服务
    /// </summary>
    public interface IRedirectService
    {
        string ExtractRedirectUriFromReturnUrl(string url);
    }
}
