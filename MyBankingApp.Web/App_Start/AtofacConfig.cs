using Autofac;
using Autofac.Integration.Mvc;
using MyBankingApp.Data.Context;
using System.Web.Mvc;

namespace MyBankingApp.Web.App_Start
{
    public static class AutofacConfig
    {
        public static void ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            // Register controllers
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            // Register Entity Framework DbContext
            builder.RegisterType<BankingDbContext>()
                .AsSelf()
                .InstancePerRequest();

            // Keine Repository-Registrierungen mehr!

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}