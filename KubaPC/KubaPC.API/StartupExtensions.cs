using Autofac;
using KubaPC.API.Factories;

namespace KubaPC.API
{
    public static class StartupExtensions
    {
        public static void RunTimeEnvironment(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
        }
        public static void ConfigureContainer(ContainerBuilder builder)
        {
            // Register different named services directly with Autofac
            builder.RegisterModule(new Factories.Services());
        }
    }
}
