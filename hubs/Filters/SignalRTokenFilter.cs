using System.Reflection;
using Microsoft.AspNetCore.SignalR;

public class SignalRTokenFilter : IHubFilter
{
    private readonly ISignalRTokenValidator _validator;

    public SignalRTokenFilter(ISignalRTokenValidator validator) => _validator = validator;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var required =
            context.HubMethod.GetCustomAttribute<ValidateSignalRTokenAttribute>() is not null ||
            context.Hub.GetType().GetCustomAttribute<ValidateSignalRTokenAttribute>() is not null;

        if (required)
        {
            var accessToken = context.Context.GetHttpContext()?
              .Request.Cookies["signalr_token"];

            if (!_validator.IsValid(accessToken))
                throw new HubException("Invalid or expired token.");
        }

        return await next(context);
    }
}