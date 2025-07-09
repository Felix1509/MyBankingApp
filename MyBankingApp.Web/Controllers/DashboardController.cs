using System;
using System.Linq;
using System.Web.Mvc;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Web.ViewModels;
using System.Data.Entity;

namespace MyBankingApp.Web.Controllers
{
    /// <summary>
    /// Controller für die Dashboard-Übersicht
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly BankingDbContext _db;
        private readonly string _defaultUsername = "Felix"; // TODO: Durch echte Authentication ersetzen

        #region Constructor

        public DashboardController(BankingDbContext context)
        {
            _db = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Main Dashboard

        /// <summary>
        /// Hauptseite des Dashboards mit Übersicht aller wichtigen Kennzahlen
        /// </summary>
        public ActionResult Index()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = BuildDashboardViewModel(user);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.Index", ex);
                ViewBag.ErrorMessage = "Fehler beim Laden des Dashboards.";
                return View(new DashboardVM());
            }
        }

        #endregion

        #region Widget Actions (Partial Views)

        /// <summary>
        /// Widget: Kontoübersicht
        /// </summary>
        public ActionResult AccountSummaryWidget()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return PartialView("_AccountSummaryWidget", null);

                var accounts = GetUserAccounts(user.Id);
                return PartialView("_AccountSummaryWidget", accounts);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.AccountSummaryWidget", ex);
                return PartialView("_Error");
            }
        }

        /// <summary>
        /// Widget: Letzte Transaktionen
        /// </summary>
        public ActionResult RecentTransactionsWidget(Guid? accountId = null)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return PartialView("_RecentTransactionsWidget", null);

                var transactions = GetRecentTransactions(user.Id, accountId, 10);
                ViewBag.SelectedAccountId = accountId;

                return PartialView("_RecentTransactionsWidget", transactions);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.RecentTransactionsWidget", ex);
                return PartialView("_Error");
            }
        }

        /// <summary>
        /// Widget: Finanzstatistiken
        /// </summary>
        public ActionResult FinancialStatsWidget()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return PartialView("_FinancialStatsWidget", null);

                var stats = CalculateFinancialStats(user.Id);
                return PartialView("_FinancialStatsWidget", stats);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.FinancialStatsWidget", ex);
                return PartialView("_Error");
            }
        }

        #endregion

        #region AJAX Actions

        /// <summary>
        /// Aktualisiert das Recent Transactions Widget per AJAX
        /// </summary>
        [HttpPost]
        public ActionResult UpdateRecentTransactions(Guid accountId)
        {
            try
            {
                Session["SelectedDashboardAccountId"] = accountId;
                return RecentTransactionsWidget(accountId);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.UpdateRecentTransactions", ex);
                return Json(new { success = false, message = "Fehler beim Laden der Transaktionen." });
            }
        }

        /// <summary>
        /// Lädt Dashboard-Statistiken per AJAX nach
        /// </summary>
        [HttpGet]
        public ActionResult GetDashboardStats()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                    return Json(new { success = false, message = "Nicht angemeldet" }, JsonRequestBehavior.AllowGet);

                var stats = new
                {
                    totalBalance = GetTotalBalance(user.Id),
                    monthlyIncome = GetMonthlyIncome(user.Id),
                    monthlyExpenses = GetMonthlyExpenses(user.Id),
                    accountCount = GetAccountCount(user.Id)
                };

                return Json(new { success = true, data = stats }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogError("Dashboard.GetDashboardStats", ex);
                return Json(new { success = false, message = "Fehler beim Laden der Statistiken." }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Baut das komplette Dashboard ViewModel auf
        /// </summary>
        private DashboardVM BuildDashboardViewModel(Benutzer user)
        {
            var viewModel = new DashboardVM
            {
                WelcomeMessage = GetWelcomeMessage(),
                TotalBalance = GetTotalBalance(user.Id),
                TotalAccounts = GetAccountCount(user.Id),
                TransactionsThisMonth = GetTransactionCountThisMonth()
            };

            return viewModel;
        }

        /// <summary>
        /// Holt den aktuellen Benutzer (TODO: Durch echte Authentication ersetzen)
        /// </summary>
        private Benutzer GetCurrentUser()
        {
            // TODO: Implementiere echte Authentication
            // return _db.Benutzer.FirstOrDefault(u => u.Id == User.Identity.GetUserId());

            return _db.Benutzer.FirstOrDefault(b => b.Benutzername == _defaultUsername);
        }

        /// <summary>
        /// Holt alle Bankkonten eines Benutzers
        /// </summary>
        private IQueryable<Bankkonto> GetUserAccounts(Guid userId)
        {
            return _db.Kontozugriffe
                .Where(k => k.BenutzerId == userId &&
                       k.Zugriffslevel >= Common.Enums.Kontozugriffelevel.Anzeigen)
                .Select(k => k.Konto);
        }

        /// <summary>
        /// Berechnet den Gesamtsaldo über alle Konten
        /// </summary>
        private decimal GetTotalBalance(Guid userId)
        {
            var accounts = GetUserAccounts(userId).Include(k => k.Transaktionen).ToList();

            // Berechne Saldo für jedes Konto
            foreach (var account in accounts)
            {
                decimal income = account.Transaktionen
                    .Where(t => t.EmpfaengerIBAN == account.IBAN)
                    .Sum(t => (decimal?)t.Betrag) ?? 0;

                decimal expenses = account.Transaktionen
                    .Where(t => t.AbsenderIBAN == account.IBAN)
                    .Sum(t => (decimal?)t.Betrag) ?? 0;

                account.AktuellerSaldo = income - expenses;
            }

            return accounts.Sum(a => a.AktuellerSaldo);
        }

        /// <summary>
        /// Holt die letzten Transaktionen
        /// </summary>
        private IQueryable<Transaktion> GetRecentTransactions(Guid userId, Guid? accountId, int count)
        {
            var query = _db.Transaktionen.Include(t => t.Bankkonto);

            if (accountId.HasValue)
            {
                query = query.Where(t => t.BankkontoId == accountId.Value);
            }
            else
            {
                var userAccountIds = GetUserAccounts(userId).Select(a => a.Id);
                query = query.Where(t => userAccountIds.Contains(t.BankkontoId));
            }

            return query.OrderByDescending(t => t.Buchungsdatum).Take(count);
        }

        /// <summary>
        /// Berechnet monatliche Einnahmen
        /// </summary>
        private decimal GetMonthlyIncome(Guid userId)
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var userAccounts = GetUserAccounts(userId).Select(a => a.IBAN).ToList();

            return _db.Transaktionen
                .Where(t => t.Buchungsdatum >= startOfMonth &&
                       userAccounts.Contains(t.EmpfaengerIBAN))
                .Sum(t => (decimal?)t.Betrag) ?? 0;
        }

        /// <summary>
        /// Berechnet monatliche Ausgaben
        /// </summary>
        private decimal GetMonthlyExpenses(Guid userId)
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var userAccounts = GetUserAccounts(userId).Select(a => a.IBAN).ToList();

            return _db.Transaktionen
                .Where(t => t.Buchungsdatum >= startOfMonth &&
                       userAccounts.Contains(t.AbsenderIBAN))
                .Sum(t => (decimal?)t.Betrag) ?? 0;
        }

        /// <summary>
        /// Zählt die Konten eines Benutzers
        /// </summary>
        private int GetAccountCount(Guid userId)
        {
            return _db.Kontozugriffe.Count(k => k.BenutzerId == userId &&
                                           k.Zugriffslevel >= Common.Enums.Kontozugriffelevel.Anzeigen);
        }

        /// <summary>
        /// Zählt Transaktionen im aktuellen Monat
        /// </summary>
        private int GetTransactionCountThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.Transaktionen.Count(t => t.Buchungsdatum >= startOfMonth);
        }

        /// <summary>
        /// Berechnet Finanzstatistiken
        /// </summary>
        private FinancialStatsVM CalculateFinancialStats(Guid userId)
        {
            // TODO: Implementieren
            return new FinancialStatsVM();
        }

        /// <summary>
        /// Generiert zeitabhängige Begrüßung
        /// </summary>
        private string GetWelcomeMessage()
        {
            var hour = DateTime.Now.Hour;
            var user = GetCurrentUser();
            var name = user?.Benutzername ?? "Gast";

            if (hour < 12)
                return $"Guten Morgen, {name}!";
            else if (hour < 18)
                return $"Guten Tag, {name}!";
            else
                return $"Guten Abend, {name}!";
        }

        /// <summary>
        /// Loggt Fehler (TODO: Implementiere echtes Logging)
        /// </summary>
        private void LogError(string action, Exception ex)
        {
            // TODO: Implementiere NLog, Serilog oder ähnliches
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error in {action}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}