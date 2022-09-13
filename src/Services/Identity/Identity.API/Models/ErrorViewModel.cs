// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.eShopOnContainers.Services.Identity.API.Models
{
    /// <summary>
    /// 错误页面Model
    /// </summary>
    public record ErrorViewModel
    {
        /// <summary>
        /// 错误页面Model
        /// </summary>
        public ErrorMessage Error { get; set; }
    }
}