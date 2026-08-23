using System.Reflection;
using Autofac;

namespace KubaPC.API.Factories
{
    public class Services : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.Load("KubaPC.Services"))
                .Where(t => t.Name.EndsWith("Service")
                         || t.Name.EndsWith("Helper")
                         || t.Name.EndsWith("DataAccess"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}
