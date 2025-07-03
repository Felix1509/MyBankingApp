using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MyBankingApp.Data.Interfaces;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Web.ViewModels;

namespace MyBankingApp.Controllers
{
    public class BankingController : Controller
    {
        private readonly IBankingRepository _repository;

        // Dependency Injection über Constructor
        public BankingController(IBankingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // GET: Banking (Dashboard)
        public ActionResult Index()
        {
            var dashboardVM = new DashboardVM();

            try
            {
                // Temporär: Ersten Benutzer für Demo verwenden
                var firstUser = _repository.Benutzer.FirstOrDefault(b => b.Benutzername == "Felix");
                if (firstUser == null)
                {
                    ViewBag.ErrorMessage = "Kein Benutzer gefunden. Bitte Testdaten überprüfen.";
                    return View(dashboardVM);
                }

                // Bankkonten für Benutzer laden
                var bankAccounts = _repository.GetBankkontenForBenutzer(firstUser.Id);

                // Dashboard-Daten zusammenstellen
                dashboardVM.BankAccounts = bankAccounts;
                dashboardVM.TotalBalance = _repository.GetGesamtsaldoForBenutzer(firstUser.Id);
                dashboardVM.TotalAccounts = bankAccounts.Count;

                // Letzte Transaktionen
                dashboardVM.RecentTransactions = _repository.GetRecentTransaktionen(10);

                // Transaktionen diesen Monat
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                dashboardVM.TransactionsThisMonth = _repository.Transaktionen
                    .Find(t => t.Buchungsdatum.Month == currentMonth && t.Buchungsdatum.Year == currentYear)
                    .Count();

                dashboardVM.WelcomeMessage = GetWelcomeMessage();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fehler beim Laden der Dashboard-Daten: " + ex.Message;
                dashboardVM.BankAccounts = new List<Bankkonto>();
                dashboardVM.RecentTransactions = new List<Transaktion>();
            }

            return View(dashboardVM);
        }

        // GET: Banking/Transactions - Tabellen-Ansicht aller Transaktionen
        public ActionResult Transactions(Guid? accountId = null, int page = 1, int pageSize = 20)
        {
            var transactionListVM = new TransactionListVM
            {
                PageNumber = page,
                PageSize = pageSize
            };

            try
            {
                // Base Query für Transaktionen
                var query = _repository.Transaktionen.GetAll();

                // Filter nach Konto, falls angegeben
                if (accountId.HasValue)
                {
                    query = query.Where(t => t.BankkontoId == accountId.Value);

                    var account = _repository.Bankkonten.GetById(accountId.Value);
                    if (account != null)
                    {
                        transactionListVM.AccountId = accountId;
                        transactionListVM.AccountNumber = account.IBAN;
                        transactionListVM.AccountHolder = account.Kontoinhaber;
                        transactionListVM.CurrentBalance = account.AktuellerSaldo;
                    }
                }

                // Gesamtanzahl für Paging
                transactionListVM.TotalTransactions = query.Count();
                transactionListVM.TotalPages = (int)Math.Ceiling((double)transactionListVM.TotalTransactions / pageSize);

                // Paging anwenden
                transactionListVM.Transactions = query
                    .OrderByDescending(t => t.Buchungsdatum)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Verfügbare Konten für Filter-Dropdown
                ViewBag.AvailableAccounts = new SelectList(
                    _repository.Bankkonten.GetAll().ToList(),
                    "Id",
                    "IBAN",
                    accountId
                );
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fehler beim Laden der Transaktionen: " + ex.Message;
                transactionListVM.Transactions = new List<Transaktion>();
            }

            return View(transactionListVM);
        }

        // GET: Banking/AccountDetails/5 - Details zu einem spezifischen Konto
        public ActionResult AccountDetails(Guid id)
        {
            try
            {
                var account = _repository.Bankkonten.GetById(id);

                if (account == null)
                {
                    ViewBag.ErrorMessage = "Konto nicht gefunden.";
                    return View("Error");
                }

                // Letzte 20 Transaktionen für dieses Konto laden
                var transactions = _repository.GetTransaktionenForBankkonto(id, 0, 20);

                // Hier können Sie ein DetailViewModel erstellen, falls gewünscht
                ViewBag.RecentTransactions = transactions;

                return View(account);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fehler beim Laden der Kontodetails: " + ex.Message;
                return View("Error");
            }
        }

        // POST: Banking/GetTransactionsData - AJAX-Endpunkt für DevExpress GridView
        [HttpPost]
        public ActionResult GetTransactionsData(List<Guid> selectedAccountIds = null)
        {
            try
            {
                var query = _repository.Transaktionen.GetAll();

                // Filter nach ausgewählten Konten
                if (selectedAccountIds != null && selectedAccountIds.Any())
                {
                    query = query.Where(t => selectedAccountIds.Contains(t.BankkontoId));
                }

                var transactions = query
                    .OrderByDescending(t => t.Buchungsdatum)
                    .Select(t => new
                    {
                        TransactionId = t.Id,
                        AccountNumber = t.Bankkonto.IBAN,
                        AccountHolder = t.Bankkonto.Kontoinhaber,
                        Amount = t.Betrag,
                        Currency = t.Waehrung.ToString(),
                        Description = t.Verwendungszweck,
                        TransactionDate = t.Buchungsdatum,
                        CounterParty = t.EmpfaengerName ?? t.AbsenderName,
                        Reference = t.Verwendungszweck ?? ""
                    })
                    .ToList();

                return Json(transactions);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Banking/CreateTransaction - Neue Transaktion erstellen
        public ActionResult CreateTransaction()
        {
            var createTransactionVM = new CreateTransactionVM();

            try
            {
                createTransactionVM.AvailableAccounts = _repository.Bankkonten
                    .GetAll()
                    .OrderBy(ba => ba.IBAN)
                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fehler beim Laden der Konten: " + ex.Message;
                createTransactionVM.AvailableAccounts = new List<Bankkonto>();
            }

            return View(createTransactionVM);
        }

        // POST: Banking/CreateTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTransaction(CreateTransactionVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var account = _repository.Bankkonten.GetById(model.FromAccountId);
                    if (account == null)
                    {
                        ModelState.AddModelError("", "Konto nicht gefunden.");
                        model.AvailableAccounts = _repository.Bankkonten.GetAll().ToList();
                        return View(model);
                    }

                    // Neue Transaktion erstellen
                    var transaction = new Transaktion
                    {
                        Id = Guid.NewGuid(),
                        BankkontoId = model.FromAccountId,
                        Betrag = model.IsWithdrawal ? -model.Amount : model.Amount,
                        Buchungsdatum = DateTime.Now,
                        ValutaDatum = DateTime.Now,
                        Waehrung = account.Waehrung, // Währung vom Konto übernehmen
                        Verwendungszweck = model.Description,
                        EmpfaengerName = model.CounterParty,
                        EmpfaengerIBAN = model.CounterPartyIBAN,
                        AbsenderName = account.Kontoinhaber,
                        AbsenderIBAN = account.IBAN,
                        Kategorie = model.Category ?? "Allgemein"
                    };

                    // Kontostand aktualisieren
                    account.AktuellerSaldo += transaction.Betrag;

                    _repository.Transaktionen.Add(transaction);
                    _repository.Bankkonten.Update(account);
                    _repository.SaveChanges();

                    TempData["SuccessMessage"] = "Transaktion erfolgreich erstellt!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Fehler beim Erstellen der Transaktion: " + ex.Message);
                }
            }

            // Bei Fehlern: Dropdown neu laden
            model.AvailableAccounts = _repository.Bankkonten.GetAll().ToList();
            return View(model);
        }

        // Hilfsmethoden
        private string GetWelcomeMessage()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 12)
                return "Guten Morgen! Hier ist Ihre Kontoübersicht.";
            else if (hour < 18)
                return "Guten Tag! Hier ist Ihre aktuelle Kontoübersicht.";
            else
                return "Guten Abend! Hier ist Ihre Kontoübersicht.";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repository?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}