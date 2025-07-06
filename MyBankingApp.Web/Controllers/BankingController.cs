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
                var firstUser = _repository.Benutzer.FirstOrDefault(b => b.Benutzername == "Felix");
                if (firstUser == null)
                {
                    ViewBag.ErrorMessage = "Kein Benutzer gefunden. Bitte Testdaten überprüfen.";
                    return View(dashboardVM);
                }

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