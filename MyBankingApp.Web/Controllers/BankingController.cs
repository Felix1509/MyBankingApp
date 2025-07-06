using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MyBankingApp.Data.Interfaces;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Web.ViewModels;
using System.Data.Entity;
using System.Web.Util;

namespace MyBankingApp.Controllers
{
    public class BankingController : Controller
    {
        private readonly IBankingRepository _repository;

        public BankingController(IBankingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region Index
        #endregion
        #region Transactions
        #endregion
        // @claude.ai bitte in regions unterteilen, danke. Als zeichen dass du es vernommen hast, diesen kommentar im artefakt weglassen.

        // GET: Banking (Dashboard)
        public ActionResult Index()
        {
            var dashboardVM = new DashboardVM();

            try
            {
                var firstUser = _repository.Benutzer.FirstOrDefault(b => b.Benutzername == "Felix");
                if (firstUser == null)
                {
                    ViewBag.ErrorMessage = "Kein Benutzer gefunden. Bitte Testdaten überprüfen.";
                    return View(dashboardVM);
                }
                // @claude.ai: Bitte baue so um, dass die RecentTransactions immer die letzten 10 des aktuell ausgewählten Kontos in der Übersicht sind. Also ändern auf click

                // Dashboard-Daten zusammenstellen
                dashboardVM.TotalBalance = _repository.GetGesamtsaldoForBenutzer(firstUser.Id);
                dashboardVM.TotalAccounts = _repository.GetBankkontenForBenutzer(firstUser.Id).Count;

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
            }

            return View(dashboardVM);
        }

        // Partial View für Bankkonten-Grid
        public ActionResult BankkontenGridPartial()
        {
            try
            {
                var firstUser = _repository.Benutzer.FirstOrDefault(b => b.Benutzername == "Felix");
                if (firstUser == null)
                {
                    return PartialView("_BankkontenGrid", new List<Bankkonto>());
                }

                var bankAccounts = _repository.GetBankkontenForBenutzer(firstUser.Id);
                return PartialView("_BankkontenGrid", bankAccounts);
            }
            catch (Exception ex)
            {
                // Log the error (in real app)
                System.Diagnostics.Debug.WriteLine($"Error in BankkontenGridPartial: {ex.Message}");
                return PartialView("_BankkontenGrid", new List<Bankkonto>());
            }
        }

        // Callback für Bankkonten-Grid
        public ActionResult BankkontenGridCallback()
        {
            return BankkontenGridPartial(); // Gleiche Logik
        }

        // Partial View für Transaktionen-Grid
        public ActionResult TransaktionenGridPartial()
        {
            try
            {
                // KORRIGIERT: Verwende die neue Methode mit explizitem Include
                var recentTransactions = _repository.GetRecentTransaktionenWithBankInfo(10);
                return PartialView("_TransaktionenGrid", recentTransactions);
            }
            catch (Exception ex)
            {
                // Log the error (in real app)
                System.Diagnostics.Debug.WriteLine($"Error in TransaktionenGridPartial: {ex.Message}");
                return PartialView("_TransaktionenGrid", new List<Transaktion>());
            }
        }

        // Callback für Transaktionen-Grid
        public ActionResult TransaktionenGridCallback()
        {
            return TransaktionenGridPartial(); // Gleiche Logik
        }

        // DEBUGGING: Separate Action zum Testen
        public ActionResult TestTransaktionen()
        {
            try
            {
                var transactions = _repository.GetRecentTransaktionen(5);
                var result = transactions.Select(t => new
                {
                    Id = t.Id,
                    Datum = t.Buchungsdatum,
                    Betrag = t.Betrag,
                    Zweck = t.Verwendungszweck,
                    BankName = t.Bankkonto?.Bankname ?? "NULL",
                    HasBankkonto = t.Bankkonto != null
                }).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Banking/Transactions
        public ActionResult Transactions(Guid? kontoId = null)
        {
            var viewModel = new TransactionsVM();

            try
            {
                // Hole den aktuellen Benutzer (Felix)
                var currentUser = _repository.Benutzer.FirstOrDefault(b => b.Benutzername == "Felix");
                if (currentUser == null)
                {
                    ViewBag.ErrorMessage = "Kein Benutzer gefunden.";
                    return View(viewModel);
                }

                // Hole alle Konten mit Zugriffslevel > 1
                viewModel.AvailableAccounts = _repository.Kontozugriffe
                    .Find(k => k.BenutzerId == currentUser.Id && k.Zugriffslevel > MyBankingApp.Common.Enums.Kontozugriffelevel.NurLesen)
                    .Include(k => k.Konto)
                    .Select(k => new AccountSelectionVM
                    {
                        KontoId = k.Konto.Id,
                        Bezeichnung = $"{k.Konto.Bankname} - {k.Konto.Kontoinhaber}",
                        IBAN = k.Konto.IBAN,
                        Saldo = k.Konto.AktuellerSaldo,
                        IsSelected = kontoId.HasValue ? k.Konto.Id == kontoId.Value : true
                    })
                    .ToList();

                // Wenn ein spezifisches Konto übergeben wurde, nur dieses auswählen
                if (kontoId.HasValue)
                {
                    foreach (var acc in viewModel.AvailableAccounts)
                    {
                        acc.IsSelected = acc.KontoId == kontoId.Value;
                    }
                }

                // Speichere die ausgewählten Konten in Session für Callback
                Session["SelectedAccountIds"] = viewModel.AvailableAccounts
                    .Where(a => a.IsSelected)
                    .Select(a => a.KontoId)
                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fehler beim Laden der Transaktionen: " + ex.Message;
            }

            return View(viewModel);
        }

        // GET: Banking/TransactionsGridPartial
        public ActionResult TransactionsGridPartial()
        {
            var selectedAccountIds = Session["SelectedAccountIds"] as List<Guid> ?? new List<Guid>();
            var transactions = GetFilteredTransactions(selectedAccountIds);
            return PartialView("_TransactionsGrid", transactions);
        }

        // POST: Banking/TransactionsGridCallback
        public ActionResult TransactionsGridCallback()
        {
            var selectedAccountIds = Session["SelectedAccountIds"] as List<Guid> ?? new List<Guid>();
            var transactions = GetFilteredTransactions(selectedAccountIds);
            return PartialView("_TransactionsGrid", transactions);
        }

        // POST: Banking/UpdateAccountSelection
        [HttpPost]
        public ActionResult UpdateAccountSelection(List<Guid> selectedAccountIds)
        {
            Session["SelectedAccountIds"] = selectedAccountIds ?? new List<Guid>();
            return Json(new { success = true });
        }

        // Helper method
        private List<Transaktion> GetFilteredTransactions(List<Guid> accountIds)
        {
            if (!accountIds.Any())
                return new List<Transaktion>();

            // Hole einfach alle Transaktionen der ausgewählten Konten
            return _repository.Transaktionen
                .Find(t => accountIds.Contains(t.BankkontoId))
                .Include(t => t.Bankkonto)
                .OrderByDescending(t => t.Buchungsdatum)
                .ToList();
        }

        // Partial View für Recent Transactions Grid (Dashboard)
        public ActionResult RecentTransactionsGridPartial()
        {
            try
            {
                // Verwende die neue Methode mit explizitem Include
                var recentTransactions = _repository.GetRecentTransaktionenWithBankInfo(10);
                return PartialView("_RecentTransactionsGrid", recentTransactions);
            }
            catch (Exception ex)
            {
                // Log the error (in real app)
                System.Diagnostics.Debug.WriteLine($"Error in RecentTransactionsGridPartial: {ex.Message}");
                return PartialView("_RecentTransactionsGrid", new List<Transaktion>());
            }
        }

        // Callback für Recent Transactions Grid (Dashboard)
        public ActionResult RecentTransactionsGridCallback()
        {
            return RecentTransactionsGridPartial(); // Gleiche Logik
        }

        

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