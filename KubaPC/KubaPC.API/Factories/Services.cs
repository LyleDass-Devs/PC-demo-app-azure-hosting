using System.Reflection;
using Autofac;
using KubaPC.Services.Data;

namespace KubaPC.API.Factories
{
    public class Services : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // One data access instance per process: it owns the Npgsql data source, so a single
            // instance means a single connection pool and a single token refresh loop.
            builder.RegisterType<NpgsqlDataAccess>()
                .As<INpgsqlDataAccess>()
                .SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.Load("KubaPC.Services"))
                .Where(t => t != typeof(NpgsqlDataAccess)
                        && (t.Name.EndsWith("Service")
                         || t.Name.EndsWith("Helper")
                         || t.Name.EndsWith("DataAccess")))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}
