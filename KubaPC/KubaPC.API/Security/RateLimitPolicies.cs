using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace KubaPC.API.Security
{
    public static class RateLimitPolicies
    {
        public const string AUTHENTICATION = "authentication";
        public const string SENSITIVE = "sensitive";

        private const int AUTHENTICATION_REQUESTS_PER_MINUTE = 30;
        private const int SENSITIVE_REQUESTS_PER_MINUTE = 10;
        private const int GLOBAL_REQUESTS_PER_MINUTE = 300;
        private const string UNKNOWN_CLIENT = "unknown";
        private const string RETRY_AFTER_SECONDS = "60";

        public static void BuildRateLimiting(this WebApplicationBuilder builder)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) =>
                {
                    context.HttpContext.Response.Headers.RetryAfter = RETRY_AFTER_SECONDS;
                    return ValueTask.CompletedTask;
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(GLOBAL_REQUESTS_PER_MINUTE)));

                options.AddPolicy(AUTHENTICATION, context =>
                    RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(AUTHENTICATION_REQUESTS_PER_MINUTE)));

                options.AddPolicy(SENSITIVE, context =>
                    RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(SENSITIVE_REQUESTS_PER_MINUTE)));
            });
        }

        private static string ClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? UNKNOWN_CLIENT;

        private static FixedWindowRateLimiterOptions Window(int permitLimit) => new()
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        };
    }
}
