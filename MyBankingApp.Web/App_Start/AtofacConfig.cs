using Autofac;
using Autofac.Integration.Mvc;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Interfaces;
using MyBankingApp.Data.Repositories;
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
                .InstancePerRequest(); // Ein DbContext pro HTTP-Request

            // Register generic repository
            builder.RegisterGeneric(typeof(Repository<>))
                .As(typeof(IRepository<>))
                .InstancePerRequest();

            // Register specific repositories
            builder.RegisterType<BankingRepository>()
                .As<IBankingRepository>()
                .InstancePerRequest();

            // Optional: Register services (falls Sie später welche hinzufügen)
            // builder.RegisterType<BankAccountService>()
            //     .As<IBankAccountService>()
            //     .InstancePerRequest();

            // Build container
            var container = builder.Build();

            // Set dependency resolver
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}