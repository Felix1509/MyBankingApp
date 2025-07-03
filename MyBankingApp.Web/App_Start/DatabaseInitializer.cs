using MyBankingApp.Data.Context;
using System.Data.Entity;

namespace MyBankingApp.Web.App_Start
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            // Datenbank-Initialisierung strategy setzen
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<BankingDbContext, MyBankingApp.Data.Migrations.Configuration>());

            // Kontext erstellen und Testdaten einfügen
            using (var context = new BankingDbContext())
            {
                // Sicherstellen, dass Datenbank existiert
                context.Database.Initialize(true);

                // Testdaten einfügen (falls noch nicht vorhanden)
                context.InsertTestData();
            }
        }
    }
}