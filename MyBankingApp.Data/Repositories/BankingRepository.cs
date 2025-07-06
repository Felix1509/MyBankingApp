using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace MyBankingApp.Data.Repositories
{
    public class BankingRepository : IBankingRepository
    {
        private readonly BankingDbContext _context;
        private bool _disposed = false;

        public BankingRepository(BankingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Repository properties
        public IRepository<Bankkonto> Bankkonten => new Repository<Bankkonto>(_context);
        public IRepository<Transaktion> Transaktionen => new Repository<Transaktion>(_context);
        public IRepository<Benutzer> Benutzer => new Repository<Benutzer>(_context);
        public IRepository<Kontozugriff> Kontozugriffe => new Repository<Kontozugriff>(_context);
        public IRepository<Geldvorgang> Geldvorgaenge => new Repository<Geldvorgang>(_context);
        public IRepository<Beleg> Belege => new Repository<Beleg>(_context);
        public IRepository<VerkTransaktionGV> VerkTransaktionenGV => new Repository<VerkTransaktionGV>(_context);

        // Business logic methods
        public List<Bankkonto> GetBankkontenForBenutzer(Guid benutzerId)
        {
            var bankkonten = _context.Kontozugriffe
                .Where(k => k.BenutzerId == benutzerId)
                .Include(k => k.Konto)
                .Include(k => k.Konto.Transaktionen)
                .Select(k => k.Konto)
                .ToList();

            // Berechne den aktuellen Saldo für jedes Konto basierend auf Transaktionen
            foreach (var konto in bankkonten)
            {
                decimal einnahmen = konto.Transaktionen
                    .Where(t => t.EmpfaengerIBAN == konto.IBAN)
                    .Sum(t => t.Betrag);

                decimal ausgaben = konto.Transaktionen
                    .Where(t => t.AbsenderIBAN == konto.IBAN)
                    .Sum(t => t.Betrag);

                konto.AktuellerSaldo = einnahmen - ausgaben;
            }

            return bankkonten;
        }

        public List<Transaktion> GetTransaktionenForBankkonto(Guid bankkontoId, int skip = 0, int take = 20)
        {
            return _context.Transaktionen
                .Where(t => t.BankkontoId == bankkontoId)
                .OrderByDescending(t => t.Buchungsdatum)
                .Skip(skip)
                .Take(take)
                .Include(t => t.Bankkonto)
                .ToList();
        }

        public List<Transaktion> GetRecentTransaktionen(int count = 10)
        {
            return _context.Transaktionen
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .ToList();
        }

        public List<Transaktion> GetRecentTransaktionenWithBankInfo(int count = 10)
        {
            // Hole die letzten X Transaktionen, unabhängig vom Monat
            return _context.Transaktionen
                .Include(t => t.Bankkonto)
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .ToList();
        }

        public decimal GetGesamtsaldoForBenutzer(Guid benutzerId)
        {
            var konten = GetBankkontenForBenutzer(benutzerId);
            return konten.Sum(k => k.AktuellerSaldo);
        }

        // Unit of Work pattern
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void BeginTransaction()
        {
            _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _context.Database.CurrentTransaction?.Commit();
        }

        public void RollbackTransaction()
        {
            _context.Database.CurrentTransaction?.Rollback();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}