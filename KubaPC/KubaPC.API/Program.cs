using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;
using KubaPC.API.Middleware;
using KubaPC.API.Security;

namespace KubaPC.API
{
    public static class Program
    {
        private const long MAXIMUM_REQUEST_BODY_BYTES = 64 * 1024;

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(StartupExtensions.ConfigureContainer);
            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MAXIMUM_REQUEST_BODY_BYTES);

            builder.BuildAuthentication();
            builder.BuildAPIAuthorization();
            builder.BuildDatabaseLogging();
            builder.BuildCors();
            builder.BuildRateLimiting();
            builder.Services.AddHealthChecks();
            builder.Services.AddFastEndpoints();

            WebApplication app = builder.Build();

            app.RunTimeEnvironment();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseCors(StartupExtensions.CLIENT_CORS_POLICY);
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<PasswordChangeMiddleware>();
            app.MapHealthChecks("/health");
            app.UseFastEndpoints();

            app.Run();
        }
    }
}
