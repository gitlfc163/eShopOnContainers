﻿namespace Microsoft.eShopOnContainers.Services.Basket.API.Services;

/// <summary>
/// 认证服务实现Service类
/// </summary>
public class IdentityService : IIdentityService
{
    private IHttpContextAccessor _context;

    public IdentityService(IHttpContextAccessor context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string GetUserIdentity()
    {
        return _context.HttpContext.User.FindFirst("sub").Value;
    }
}

