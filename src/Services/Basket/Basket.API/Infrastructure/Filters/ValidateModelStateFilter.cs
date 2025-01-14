﻿namespace Basket.API.Infrastructure.Filters;

/// <summary>
/// 模型验证过滤器
/// </summary>
public class ValidateModelStateFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var validationErrors = context.ModelState
            .Keys
            .SelectMany(k => context.ModelState[k].Errors)
            .Select(e => e.ErrorMessage)
            .ToArray();

        var json = new JsonErrorResponse
        {
            Messages = validationErrors
        };

        context.Result = new BadRequestObjectResult(json);
    }
}

