namespace MyBankingApp.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bankkonten",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Bezeichnung = c.String(),
                        IBAN = c.String(nullable: false, maxLength: 500),
                        BIC = c.String(),
                        Bankname = c.String(),
                        BLZ = c.String(),
                        Kontonummer = c.String(),
                        Kontotyp = c.Int(nullable: false),
                        Waehrung = c.Int(nullable: false),
                        Kontoinhaber = c.String(),
                        AktuellerSaldo = c.Decimal(nullable: false, precision: 15, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Transaktionen",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Buchungsdatum = c.DateTime(nullable: false),
                        ValutaDatum = c.DateTime(nullable: false),
                        Betrag = c.Decimal(nullable: false, precision: 15, scale: 2),
                        Waehrung = c.Int(nullable: false),
                        EmpfaengerName = c.String(),
                        EmpfaengerIBAN = c.String(),
                        AbsenderName = c.String(),
                        AbsenderIBAN = c.String(),
                        Verwendungszweck = c.String(),
                        Kategorie = c.String(),
                        BankkontoId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Bankkonten", t => t.BankkontoId)
                .Index(t => t.BankkontoId);
            
            CreateTable(
                "dbo.VerkTransaktionenGV",
                c => new
                    {
                        GeldvorgangId = c.Guid(nullable: false),
                        TransaktionId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.GeldvorgangId, t.TransaktionId })
                .ForeignKey("dbo.Geldvorgaenge", t => t.GeldvorgangId, cascadeDelete: true)
                .ForeignKey("dbo.Transaktionen", t => t.TransaktionId, cascadeDelete: true)
                .Index(t => t.GeldvorgangId)
                .Index(t => t.TransaktionId);
            
            CreateTable(
                "dbo.Geldvorgaenge",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Datum = c.DateTime(),
                        Bezeichnung = c.String(),
                        BelegId = c.Guid(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Belege", t => t.BelegId)
                .Index(t => t.BelegId);
            
            CreateTable(
                "dbo.Belege",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Bezeichnung = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Benutzer",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Benutzername = c.String(),
                        Passwort = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Kontozugriffe",
                c => new
                    {
                        KontoId = c.Guid(nullable: false),
                        BenutzerId = c.Guid(nullable: false),
                        Zugriffslebel = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.KontoId, t.BenutzerId })
                .ForeignKey("dbo.Benutzer", t => t.BenutzerId, cascadeDelete: true)
                .ForeignKey("dbo.Bankkonten", t => t.KontoId, cascadeDelete: true)
                .Index(t => t.KontoId)
                .Index(t => t.BenutzerId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Kontozugriffe", "KontoId", "dbo.Bankkonten");
            DropForeignKey("dbo.Kontozugriffe", "BenutzerId", "dbo.Benutzer");
            DropForeignKey("dbo.VerkTransaktionenGV", "TransaktionId", "dbo.Transaktionen");
            DropForeignKey("dbo.VerkTransaktionenGV", "GeldvorgangId", "dbo.Geldvorgaenge");
            DropForeignKey("dbo.Geldvorgaenge", "BelegId", "dbo.Belege");
            DropForeignKey("dbo.Transaktionen", "BankkontoId", "dbo.Bankkonten");
            DropIndex("dbo.Kontozugriffe", new[] { "BenutzerId" });
            DropIndex("dbo.Kontozugriffe", new[] { "KontoId" });
            DropIndex("dbo.Geldvorgaenge", new[] { "BelegId" });
            DropIndex("dbo.VerkTransaktionenGV", new[] { "TransaktionId" });
            DropIndex("dbo.VerkTransaktionenGV", new[] { "GeldvorgangId" });
            DropIndex("dbo.Transaktionen", new[] { "BankkontoId" });
            DropTable("dbo.Kontozugriffe");
            DropTable("dbo.Benutzer");
            DropTable("dbo.Belege");
            DropTable("dbo.Geldvorgaenge");
            DropTable("dbo.VerkTransaktionenGV");
            DropTable("dbo.Transaktionen");
            DropTable("dbo.Bankkonten");
        }
    }
}
