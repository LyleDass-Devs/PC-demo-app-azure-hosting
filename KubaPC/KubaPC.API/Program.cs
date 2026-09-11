using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;

namespace KubaPC.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(StartupExtensions.ConfigureContainer);

            builder.BuildAuthentication();
            builder.BuildAPIAuthorization();
            builder.BuildDatabaseLogging();
            builder.BuildCors();
            builder.Services.AddFastEndpoints();

            WebApplication app = builder.Build();

            app.RunTimeEnvironment();
            app.UseCors(StartupExtensions.CLIENT_CORS_POLICY);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<KubaPC.API.Middleware.PasswordChangeMiddleware>();
            app.UseFastEndpoints();

            app.Run();
        }
    }
}