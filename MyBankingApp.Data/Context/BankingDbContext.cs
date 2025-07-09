using MyBankingApp.Common.Enums;
using MyBankingApp.Data.Entitites;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;

namespace MyBankingApp.Data.Context
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext() : base("DefaultConnection")
        {
            Configuration.LazyLoadingEnabled = true;
        }

        public DbSet<Bankkonto> Bankkonten { get; set; }
        public DbSet<Transaktion> Transaktionen { get; set; }
        public DbSet<Benutzer> Benutzer { get; set; }
        public DbSet<Kontozugriff> Kontozugriffe { get; set; }
        public DbSet<Geldvorgang> Geldvorgaenge { get; set; }
        public DbSet<Beleg> Belege { get; set; }
        public DbSet<VerkTransaktionGV> VerkTransaktionenGV { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bankkonto>().ToTable("Bankkonten");
            modelBuilder.Entity<Benutzer>().ToTable("Benutzer");
            modelBuilder.Entity<Beleg>().ToTable("Belege");
            modelBuilder.Entity<Geldvorgang>().ToTable("Geldvorgaenge");
            modelBuilder.Entity<Kontozugriff>().ToTable("Kontozugriffe");
            modelBuilder.Entity<Transaktion>().ToTable("Transaktionen");
            modelBuilder.Entity<VerkTransaktionGV>().ToTable("VerkTransaktionenGV");
            // Composite Keys
            modelBuilder.Entity<Kontozugriff>()
                .HasKey(k => new { k.KontoId, k.BenutzerId });

            modelBuilder.Entity<VerkTransaktionGV>()
                .HasKey(v => new { v.GeldvorgangId, v.TransaktionId });

            // Decimal Precision
            modelBuilder.Entity<Bankkonto>()
                .Property(b => b.AktuellerSaldo)
                .HasPrecision(15, 2);

            modelBuilder.Entity<Transaktion>()
                .Property(t => t.Betrag)
                .HasPrecision(15, 2);

            // Relationships
            modelBuilder.Entity<Transaktion>()
                .HasRequired(t => t.Bankkonto)
                .WithMany(b => b.Transaktionen)
                .HasForeignKey(t => t.BankkontoId)
                .WillCascadeOnDelete(false);

            // WICHTIG für Verschlüsselung
            modelBuilder.Entity<Bankkonto>()
                .Property(b => b.IBAN)
                .HasMaxLength(500); // Verschlüsselt wird's länger!
        }


        public void InsertTestData()
        {
            InsertTestBankkonten();
            InsertTestUser();
            InsertTestKontozugriffe();
            InsertTestTransaktionen();
            InsertTestBelege();
            InsertTestGeldvorgaenge();
            InsertTestVerkTransaktionenGV();
        }
        private void InsertTestBankkonten()
        {
            if (Bankkonten.Any()) return;   // Aussteigen, falls es schon welche gibt
            var random = new Random();
            using (var trans = this.Database.BeginTransaction())    // Transaktionen wichtig für große Datenmengen
            {
                try
                {
                    for (int i = 1; i < 11; i++)
                    {
                        var bankkonto = new Bankkonto
                        {
                            Id = Guid.NewGuid(),
                            IBAN = $"DE{random.Next(10, 99)}{(1000000000000000000L + i).ToString().PadLeft(20, '0')}",
                            BIC = "GENODEF1XXX",
                            Bankname = $"Bank {i.ToString("D2")}",
                            BLZ = random.Next(10000000, 99999999).ToString(),
                            Kontonummer = random.Next(100000000, 999999999).ToString(),
                            Kontotyp = (Kontotyp)(i % 7), // oder (int)Kontotyp.Girokonto etc., je nach Enum
                            Waehrung = (Waehrung)(i % 10), // oder (int)Waehrung.EUR etc.
                            Kontoinhaber = $"Inhaber {i.ToString("D2")}"
                        };
                        Bankkonten.Add(bankkonto);
                        SaveChanges();
                    }
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                }
            }

        }
        private void InsertTestUser()
        {
            if (Benutzer.Any()) return;   // Aussteigen, falls es schon welche gibt
            try
            {
                var myUser = new Benutzer()
                {
                    Id = Guid.NewGuid(),
                    Benutzername = "Felix",
                    Passwort = "1234"
                };
                Benutzer.Add(myUser);
                SaveChanges();
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung
                Console.WriteLine($"Fehler beim Einfügen von Testbenutzern: {ex.Message}");
            }
        }

        private void InsertTestKontozugriffe()
        {
            if (Kontozugriffe.Any()) return;   // Aussteigen, falls es schon welche gibt
            try
            {
                var myUser = Benutzer.FirstOrDefault(x => x.Benutzername == "Felix");
                int i = 0;
                List<Bankkonto> KontoListe = Bankkonten.ToList();
                foreach (var bankkonto in KontoListe)
                {
                    var Zugriiff = new Kontozugriff()
                    {
                        Konto = bankkonto,
                        Benutzer = myUser,
                        Zugriffslevel = (Kontozugriffelevel)(i % 6)
                    };
                    i++;
                    if (Zugriiff.Zugriffslevel != Kontozugriffelevel.Keiner) Kontozugriffe.Add(Zugriiff);
                }
                SaveChanges();
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung
                Console.WriteLine($"Fehler beim Einfügen von Testzugriffen: {ex.Message}");
            }
        }
        private void InsertTestTransaktionen()
        {

            var konten = Bankkonten.ToList();
            int zaehler = 1;
            using (var trans = Database.BeginTransaction())
            {
                foreach (var empfaengerKonto in konten)
                {
                    if (Transaktionen.Any(t => t.Bankkonto.Id == empfaengerKonto.Id)) continue; // Skip if transactions already exist for this account
                    for (int i = 1; i < 11; i++)
                    {
                        Thread.Sleep(1);
                        var AbsenderKonto = GetZufaelligesBankkonto(konten);
                        var Transaktion = new Transaktion()
                        {
                            Id = Guid.NewGuid(),
                            Buchungsdatum = DateTime.Now.AddDays(-i).AddMonths(-i),
                            ValutaDatum = DateTime.Now.AddDays(-(i - 1)).AddMonths(-i),
                            Betrag = Convert.ToDecimal(zaehler.ToString() + i.ToString() + "," + i.ToString()),
                            Waehrung = (Waehrung)(i % 10),
                            EmpfaengerName = empfaengerKonto.Kontoinhaber,
                            EmpfaengerIBAN = empfaengerKonto.IBAN,
                            AbsenderIBAN = AbsenderKonto.IBAN,
                            AbsenderName = AbsenderKonto.Kontoinhaber,
                            Verwendungszweck = "Verwendungszweck " + zaehler,
                            Kategorie = "Kategorie " + i,
                            Bankkonto = empfaengerKonto
                        };
                        Transaktionen.Add(Transaktion);
                        // Reverse transaktion auch einmal hinzufüpgen, damit nicht nur gutschriften da sind
                        var ZahlungsTrans = new Transaktion()
                        {
                            Id = Guid.NewGuid(),
                            Buchungsdatum = Transaktion.Buchungsdatum,
                            ValutaDatum = Transaktion.ValutaDatum,
                            Betrag = Transaktion.Betrag,
                            Waehrung = Transaktion.Waehrung,
                            EmpfaengerIBAN = Transaktion.AbsenderIBAN,
                            EmpfaengerName = Transaktion.AbsenderName,
                            AbsenderIBAN = Transaktion.EmpfaengerIBAN,
                            AbsenderName = Transaktion.EmpfaengerName,
                            Verwendungszweck = "Verwendungszweck" + zaehler + " - Reversed",
                            Kategorie = "Kategorie " + i,
                            Bankkonto = AbsenderKonto
                        };
                        zaehler++;
                    }
                    SaveChanges();
                }
                try
                {
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                }
            }

        }

        private void InsertTestBelege()
        {
            if (Belege.Any()) return;   // Aussteigen, falls es schon welche gibt
            for (int i = 1; i < 11; i++)
            {
                var beleg = new Beleg()
                {
                    Id = Guid.NewGuid(),
                    Bezeichnung = "Beleg " + i
                };
                Belege.Add(beleg);
            }
            SaveChanges();
        }
        private void InsertTestGeldvorgaenge()
        {
            if (Geldvorgaenge.Any()) return;   // Aussteigen, falls es schon welche gibt    
            for (int i = 1; i < 11; i++)
            {
                var GV = new Geldvorgang()
                {
                    Id = Guid.NewGuid(),
                    Datum = DateTime.Now.AddDays(-i).AddMonths(-i),
                    Bezeichnung = "Geldvorgang " + i,
                    BelegId = (i % 2 == 0) ? Belege.FirstOrDefault(x => x.Bezeichnung == "Beleg " + i)?.Id : null, // Nur jeder zweite Geldvorgang hat einen Beleg

                };
                Geldvorgaenge.Add(GV);
            }
            SaveChanges();
        }
        private void InsertTestVerkTransaktionenGV()
        {
            if (VerkTransaktionenGV.Any()) return;   // Aussteigen, falls es schon welche gibt
            var transactions = Transaktionen.ToList();
            List<Geldvorgang> gvList = Geldvorgaenge.ToList();
            foreach (var gv in gvList)
            {
                var trans = GetZufaelligeTransaktion(transactions);
                gv.Bezeichnung = "Rechnung über " + trans.Betrag.ToString() + trans.Waehrung.ToString();
                SaveChanges();
                var Verk = new VerkTransaktionGV()
                {
                    Geldvorgang = gv,
                    Transaktion = trans
                };
                VerkTransaktionenGV.Add(Verk);
            }
            SaveChanges();
        }
        private static Bankkonto GetZufaelligesBankkonto(List<Bankkonto> konten)
        {
            var random = new Random();
            int index = random.Next(konten.Count);
            return konten[index];
        }
        private static Transaktion GetZufaelligeTransaktion(List<Transaktion> konten)
        {
            var random = new Random();
            int index = random.Next(konten.Count);
            return konten[index];
        }
    }
}
