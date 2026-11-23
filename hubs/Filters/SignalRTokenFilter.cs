using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using biddingServer.Attributes;
public sealed class SignalRTokenFilter : IHubFilter
{
    private readonly ISignalRTokenValidator _validator;

    public SignalRTokenFilter(ISignalRTokenValidator validator) => _validator = validator;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var attrProps =
            context.HubMethod.GetCustomAttribute<ValidateSignalRTokenAttribute>() is not null ||
            context.Hub.GetType().GetCustomAttribute<ValidateSignalRTokenAttribute>() is not null;

        var attr = context.HubMethod.GetCustomAttribute<ValidateSignalRTokenAttribute>();

        if (attrProps)
        {
            var accessToken = context.Context.GetHttpContext()?
              .Request.Cookies["signalr_token"];

            if (!_validator.IsValid(accessToken))
                throw new HubException("Unauthorized: Invalid or expired token.");

        }

        if (attr is not null && !attr.RoleCheck (context.Context.User))
            throw new HubException("Unauthorized:Your role is insufficient");

        return await next(context);
    }
}