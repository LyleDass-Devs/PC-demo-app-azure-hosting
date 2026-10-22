using Autofac;
using System.Reflection;

namespace KubaPC.SharedClient.Services.Factories
{

    public class Services : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.Name.EndsWith("Service")
                            || type.Name.EndsWith("Store")
                            || type.Name.EndsWith("Helper")
                            || type.Name.EndsWith("Activity"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}
