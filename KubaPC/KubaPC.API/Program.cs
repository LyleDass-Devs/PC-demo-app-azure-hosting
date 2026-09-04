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
            builder.Services.AddFastEndpoints();

            WebApplication app = builder.Build();

            app.RunTimeEnvironment();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseFastEndpoints();

            app.Run();
        }
    }
}