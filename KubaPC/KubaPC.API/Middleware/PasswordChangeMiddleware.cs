namespace KubaPC.API.Middleware
{
    public class PasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isChangeRequired = context.User.Identity?.IsAuthenticated == true && context.User.HasClaim("PasswordChangeRequired", "true");

            if (isChangeRequired && !context.Request.Path.StartsWithSegments("/accounts/change-password"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new { error = "Password change required before continuing." });
                return;
            }

            await _next(context);
        }

    }
}
