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


        public List<Transaktion> GetTransaktionenForBankkonto(Guid bankkontoId, int skip = 0, int take = 20)
        {
            return _context.Transaktionen
                .Where(t => t.BankkontoId == bankkontoId)
                .OrderByDescending(t => t.Buchungsdatum)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
        public List<Bankkonto> GetBankkontenForBenutzer(Guid benutzerId)
        {
            var konten = _context.Kontozugriffe
                .Where(ka => ka.BenutzerId == benutzerId && ka.Zugriffslebel >= Kontozugriffelevel.Anzeigen)
                .Include(ka => ka.Konto)
                .Include(ka => ka.Konto.Transaktionen) // WICHTIG: Transaktionen mit laden
                .Select(ka => ka.Konto)
                .ToList();

            // Saldo berechnen
            foreach (var konto in konten)
            {
                konto.AktuellerSaldo = konto.Transaktionen.Sum(x => x.Betrag);
            }

            return konten;
        }

        public List<Transaktion> GetRecentTransaktionen(int count = 10)
        {
            return _context.Transaktionen
                .Include(t => t.Bankkonto) // WICHTIG: Bankkonto mit laden
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .ToList();
        }

        // NEUE METHODE: Speziell für Transaktionen-Grid
        public List<Transaktion> GetRecentTransaktionenWithBankInfo(int count = 10)
        {
            return _context.Transaktionen
                .Include(t => t.Bankkonto) // Bankkonto-Daten mit laden
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .Select(t => new Transaktion
                {
                    Id = t.Id,
                    Buchungsdatum = t.Buchungsdatum,
                    ValutaDatum = t.ValutaDatum,
                    Betrag = t.Betrag,
                    Waehrung = t.Waehrung,
                    Verwendungszweck = t.Verwendungszweck,
                    EmpfaengerName = t.EmpfaengerName,
                    AbsenderName = t.AbsenderName,
                    Kategorie = t.Kategorie,
                    BankkontoId = t.BankkontoId,
                    // Bankkonto-Daten explizit setzen
                    Bankkonto = new Bankkonto
                    {
                        Id = t.Bankkonto.Id,
                        Bankname = t.Bankkonto.Bankname,
                        IBAN = t.Bankkonto.IBAN,
                        Kontoinhaber = t.Bankkonto.Kontoinhaber
                    }
                })
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