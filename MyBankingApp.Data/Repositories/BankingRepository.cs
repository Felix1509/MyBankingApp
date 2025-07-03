using MyBankingApp.Common.Enums;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace MyBankingApp.Data.Repositories
{
    public class BankingRepository : IBankingRepository, IDisposable
    {
        private readonly BankingDbContext _context;
        private DbContextTransaction _transaction;

        // Repository instances
        private IRepository<Bankkonto> _bankkontenRepository;
        private IRepository<Transaktion> _transaktionenRepository;
        private IRepository<Benutzer> _benutzerRepository;
        private IRepository<Kontozugriff> _kontozugriffeRepository;
        private IRepository<Geldvorgang> _geldvorgaengeRepository;
        private IRepository<Beleg> _belegeRepository;
        private IRepository<VerkTransaktionGV> _verkTransaktionenGVRepository;

        public BankingRepository(BankingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Repository properties with lazy loading
        public IRepository<Bankkonto> Bankkonten
        {
            get { return _bankkontenRepository ?? (_bankkontenRepository = new Repository<Bankkonto>(_context)); }
        }

        public IRepository<Transaktion> Transaktionen
        {
            get { return _transaktionenRepository ?? (_transaktionenRepository = new Repository<Transaktion>(_context)); }
        }

        public IRepository<Benutzer> Benutzer
        {
            get { return _benutzerRepository ?? (_benutzerRepository = new Repository<Benutzer>(_context)); }
        }

        public IRepository<Kontozugriff> Kontozugriffe
        {
            get { return _kontozugriffeRepository ?? (_kontozugriffeRepository = new Repository<Kontozugriff>(_context)); }
        }

        public IRepository<Geldvorgang> Geldvorgaenge
        {
            get { return _geldvorgaengeRepository ?? (_geldvorgaengeRepository = new Repository<Geldvorgang>(_context)); }
        }

        public IRepository<Beleg> Belege
        {
            get { return _belegeRepository ?? (_belegeRepository = new Repository<Beleg>(_context)); }
        }

        public IRepository<VerkTransaktionGV> VerkTransaktionenGV
        {
            get { return _verkTransaktionenGVRepository ?? (_verkTransaktionenGVRepository = new Repository<VerkTransaktionGV>(_context)); }
        }

        // Business logic methods
        public List<Bankkonto> GetBankkontenForBenutzer(Guid benutzerId)
        {
            return _context.Kontozugriffe
                .Where(ka => ka.BenutzerId == benutzerId && ka.Zugriffslebel >= Kontozugriffelevel.Anzeigen)
                .Include(ka => ka.Konto)
                .Select(ka => ka.Konto)
                .ToList();
        }

        public List<Transaktion> GetTransaktionenForBankkonto(Guid bankkontoId, int skip = 0, int take = 20)
        {
            return _context.Transaktionen
                .Where(t => t.BankkontoId == bankkontoId)
                .OrderByDescending(t => t.Buchungsdatum)
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        public List<Transaktion> GetRecentTransaktionen(int count = 10)
        {
            return _context.Transaktionen
                .Include(t => t.Bankkonto)
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .ToList();
        }

        public decimal GetGesamtsaldoForBenutzer(Guid benutzerId)
        {
            var bankkonten = GetBankkontenForBenutzer(benutzerId);
            return bankkonten.Sum(b => b.AktuellerSaldo);
        }

        // Unit of Work pattern
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            try
            {
                SaveChanges();
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}