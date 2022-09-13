namespace Microsoft.eShopOnContainers.Services.Identity.API.Services
{
    /// <summary>
    /// 用户登录服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ILoginService<T>
    {
        Task<bool> ValidateCredentials(T user, string password);

        Task<T> FindByUsername(string user);

        Task SignIn(T user);

        Task SignInAsync(T user, AuthenticationProperties properties, string authenticationMethod = null);
    }
}
