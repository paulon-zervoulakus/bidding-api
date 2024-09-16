namespace Middleware.SignalR
{
    public class SignalRMiddleware
    {
        private readonly RequestDelegate _next;

        public SignalRMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Check if the request is for the SignalR Hub
            if (context.Request.Path.StartsWithSegments("/subastaHub"))
            {
                // You can inspect the request here (e.g., check cookies, headers)
                var token = context.Request.Cookies["signalr_token"];

                if (string.IsNullOrEmpty(token))
                {
                    // Handle the case where the token is missing
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await _next(context);
        }
    }
}