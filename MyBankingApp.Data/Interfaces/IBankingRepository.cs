using MyBankingApp.Data.Entitites;
using System;
using System.Collections.Generic;

namespace MyBankingApp.Data.Interfaces
{
    public interface IBankingRepository
    {
        // Bankkonto operations
        IRepository<Bankkonto> Bankkonten { get; }
        IRepository<Transaktion> Transaktionen { get; }
        IRepository<Benutzer> Benutzer { get; }
        IRepository<Kontozugriff> Kontozugriffe { get; }
        IRepository<Geldvorgang> Geldvorgaenge { get; }
        IRepository<Beleg> Belege { get; }
        IRepository<VerkTransaktionGV> VerkTransaktionenGV { get; }

        // Business logic methods
        List<Bankkonto> GetBankkontenForBenutzer(Guid benutzerId);
        List<Transaktion> GetTransaktionenForBankkonto(Guid bankkontoId, int skip = 0, int take = 20);
        List<Transaktion> GetRecentTransaktionen(int count = 10);
        decimal GetGesamtsaldoForBenutzer(Guid benutzerId);

        // Unit of Work pattern
        int SaveChanges();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        void Dispose();
    }
}