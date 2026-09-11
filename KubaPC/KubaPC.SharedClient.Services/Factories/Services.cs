using Autofac;
using System.Reflection;

namespace KubaPC.SharedClient.Services.Factories
{
    /// Registers the shared client services with Autofac. Both client
    /// applications load this module, so neither maintains its own list.
    public class Services : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.Name.EndsWith("Service")
                            || type.Name.EndsWith("Store")
                            || type.Name.EndsWith("Helper"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}