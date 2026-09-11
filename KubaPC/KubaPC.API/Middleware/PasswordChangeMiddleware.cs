namespace KubaPC.API.Middleware
{
    public class PasswordChangeMiddleware
    {
        private const string PASSWORD_CHANGE_ERROR_CODE = "PasswordChangeRequired";
        private const string CHANGE_PASSWORD_PATH = "/accounts/change-password";
        private readonly RequestDelegate _next;

        public PasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isChangeRequired = context.User.Identity?.IsAuthenticated == true
                                     && context.User.HasClaim("PasswordChangeRequired", "true");

            if (isChangeRequired && !context.Request.Path.StartsWithSegments(CHANGE_PASSWORD_PATH))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = PASSWORD_CHANGE_ERROR_CODE,
                    message = "Password change required before continuing."
                });

                return;
            }

            await _next(context);
        }

    }
}
