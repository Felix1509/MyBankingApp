using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Web.ViewModels;
using System.Data.Entity;
using MyBankingApp.Common.Enums;

namespace MyBankingApp.Web.Controllers
{
    /// <summary>
    /// Controller für Bankkontoverwaltung
    /// </summary>
    public class BankAccountController : Controller
    {
        private readonly BankingDbContext _db;
        private readonly string _defaultUsername = "Felix"; // TODO: Durch echte Authentication ersetzen

        #region Constructor

        public BankAccountController(BankingDbContext context)
        {
            _db = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region List & Display Actions

        /// <summary>
        /// Zeigt alle Bankkonten des Benutzers
        /// </summary>
        public ActionResult Index()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Account");

                var accounts = GetUserAccountsWithDetails(user.Id);
                return View(accounts);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.Index", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Grid für Bankkonten (Partial View)
        /// </summary>
        public ActionResult AccountGrid()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return PartialView("_AccountGrid", new List<Bankkonto>());

                var accounts = GetUserAccountsWithDetails(user.Id);
                return PartialView("_AccountGrid", accounts);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.AccountGrid", ex);
                return PartialView("_AccountGrid", new List<Bankkonto>());
            }
        }

        /// <summary>
        /// DevExpress Grid Callback
        /// </summary>
        public ActionResult AccountGridCallback()
        {
            return AccountGrid();
        }

        /// <summary>
        /// Zeigt Details eines Bankkontos
        /// </summary>
        public ActionResult Details(Guid id)
        {
            try
            {
                var account = GetAccountWithFullDetails(id);
                if (account == null) return HttpNotFound();

                if (!UserHasAccessToAccount(id))
                    return new HttpUnauthorizedResult();

                var viewModel = new BankAccountDetailsVM
                {
                    Account = account,
                    MonthlyStats = CalculateMonthlyStats(id),
                    RecentTransactions = GetRecentTransactions(id, 20),
                    AccessLevel = GetUserAccessLevel(id)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.Details", ex);
                return View("Error");
            }
        }

        #endregion

        #region Create & Edit Actions

        /// <summary>
        /// Formular zum Hinzufügen eines neuen Kontos
        /// </summary>
        public ActionResult Create()
        {
            try
            {
                var viewModel = new CreateBankAccountVM
                {
                    AvailableCurrencies = GetCurrencySelectList(),
                    AvailableAccountTypes = GetAccountTypeSelectList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.Create", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Verarbeitet neues Bankkonto
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateBankAccountVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableCurrencies = GetCurrencySelectList();
                    model.AvailableAccountTypes = GetAccountTypeSelectList();
                    return View(model);
                }

                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Account");

                // Erstelle neues Bankkonto
                var account = new Bankkonto
                {
                    Id = Guid.NewGuid(),
                    IBAN = model.IBAN,
                    BIC = model.BIC,
                    Bankname = model.BankName,
                    Kontoinhaber = model.AccountHolder,
                    Kontotyp = model.AccountType,
                    Waehrung = model.Currency,
                    Bezeichnung = model.Description,
                    AktuellerSaldo = 0
                };

                _db.Bankkonten.Add(account);

                // Erstelle Kontozugriff für den Benutzer
                var access = new Kontozugriff
                {
                    KontoId = account.Id,
                    BenutzerId = user.Id,
                    Zugriffslevel = Kontozugriffelevel.Admin
                };

                _db.Kontozugriffe.Add(access);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Bankkonto wurde erfolgreich hinzugefügt.";
                return RedirectToAction("Details", new { id = account.Id });
            }
            catch (Exception ex)
            {
                LogError("BankAccount.Create", ex);
                ModelState.AddModelError("", "Fehler beim Erstellen des Bankkontos.");
                return View(model);
            }
        }

        /// <summary>
        /// Formular zum Bearbeiten eines Kontos
        /// </summary>
        public ActionResult Edit(Guid id)
        {
            try
            {
                var account = _db.Bankkonten.Find(id);
                if (account == null) return HttpNotFound();

                if (!UserHasAccessLevel(id, Kontozugriffelevel.Admin))
                    return new HttpUnauthorizedResult();

                var viewModel = new EditBankAccountVM
                {
                    Id = account.Id,
                    Description = account.Bezeichnung,
                    AccountHolder = account.Kontoinhaber,
                    AvailableCurrencies = GetCurrencySelectList(),
                    AvailableAccountTypes = GetAccountTypeSelectList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.Edit", ex);
                return View("Error");
            }
        }

        #endregion

        #region Access Management

        /// <summary>
        /// Verwaltet Zugriffsrechte für ein Konto
        /// </summary>
        public ActionResult ManageAccess(Guid id)
        {
            try
            {
                if (!UserHasAccessLevel(id, Kontozugriffelevel.Admin))
                    return new HttpUnauthorizedResult();

                var account = _db.Bankkonten.Find(id);
                if (account == null) return HttpNotFound();

                var viewModel = new ManageAccountAccessVM
                {
                    Account = account,
                    CurrentAccess = GetAccountAccess(id),
                    AvailableUsers = GetAvailableUsersForAccess(id)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.ManageAccess", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Gewährt Zugriff auf ein Konto
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GrantAccess(Guid accountId, Guid userId, Kontozugriffelevel level)
        {
            try
            {
                if (!UserHasAccessLevel(accountId, Kontozugriffelevel.Admin))
                    return new HttpUnauthorizedResult();

                var existingAccess = _db.Kontozugriffe
                    .FirstOrDefault(k => k.KontoId == accountId && k.BenutzerId == userId);

                if (existingAccess != null)
                {
                    existingAccess.Zugriffslevel = level;
                }
                else
                {
                    _db.Kontozugriffe.Add(new Kontozugriff
                    {
                        KontoId = accountId,
                        BenutzerId = userId,
                        Zugriffslevel = level
                    });
                }

                _db.SaveChanges();

                TempData["SuccessMessage"] = "Zugriffsrechte wurden aktualisiert.";
                return RedirectToAction("ManageAccess", new { id = accountId });
            }
            catch (Exception ex)
            {
                LogError("BankAccount.GrantAccess", ex);
                TempData["ErrorMessage"] = "Fehler beim Aktualisieren der Zugriffsrechte.";
                return RedirectToAction("ManageAccess", new { id = accountId });
            }
        }

        #endregion

        #region AJAX Actions

        /// <summary>
        /// Lädt Kontostand per AJAX
        /// </summary>
        [HttpGet]
        public ActionResult GetBalance(Guid id)
        {
            try
            {
                if (!UserHasAccessToAccount(id))
                    return Json(new { success = false, message = "Kein Zugriff" }, JsonRequestBehavior.AllowGet);

                var balance = CalculateCurrentBalance(id);
                return Json(new { success = true, balance = balance }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.GetBalance", ex);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Lädt Mini-Statement per AJAX
        /// </summary>
        [HttpGet]
        public ActionResult GetMiniStatement(Guid id)
        {
            try
            {
                if (!UserHasAccessToAccount(id))
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var transactions = GetRecentTransactions(id, 5);
                var data = transactions.Select(t => new
                {
                    date = t.Buchungsdatum.ToString("dd.MM.yyyy"),
                    description = t.Verwendungszweck,
                    amount = t.Betrag,
                    isCredit = t.IstGutschrift
                });

                return Json(new { success = true, transactions = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogError("BankAccount.GetMiniStatement", ex);
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Private Helper Methods

        private Benutzer GetCurrentUser()
        {
            return _db.Benutzer.FirstOrDefault(b => b.Benutzername == _defaultUsername);
        }

        private List<Bankkonto> GetUserAccountsWithDetails(Guid userId)
        {
            var accounts = _db.Kontozugriffe
                .Where(k => k.BenutzerId == userId && k.Zugriffslevel > Kontozugriffelevel.Keiner)
                .Include(k => k.Konto)
                .Include(k => k.Konto.Transaktionen)
                .Select(k => k.Konto)
                .ToList();

            // Berechne aktuelle Salden
            foreach (var account in accounts)
            {
                account.AktuellerSaldo = CalculateCurrentBalance(account.Id);
            }

            return accounts;
        }

        private Bankkonto GetAccountWithFullDetails(Guid accountId)
        {
            return _db.Bankkonten
                .Include(b => b.Transaktionen)
                .FirstOrDefault(b => b.Id == accountId);
        }

        private bool UserHasAccessToAccount(Guid accountId)
        {
            var user = GetCurrentUser();
            if (user == null) return false;

            return _db.Kontozugriffe.Any(k =>
                k.KontoId == accountId &&
                k.BenutzerId == user.Id &&
                k.Zugriffslevel > Kontozugriffelevel.Keiner);
        }

        private bool UserHasAccessLevel(Guid accountId, Kontozugriffelevel requiredLevel)
        {
            var user = GetCurrentUser();
            if (user == null) return false;

            var access = _db.Kontozugriffe.FirstOrDefault(k =>
                k.KontoId == accountId && k.BenutzerId == user.Id);

            return access != null && access.Zugriffslevel >= requiredLevel;
        }

        private Kontozugriffelevel GetUserAccessLevel(Guid accountId)
        {
            var user = GetCurrentUser();
            if (user == null) return Kontozugriffelevel.Keiner;

            var access = _db.Kontozugriffe.FirstOrDefault(k =>
                k.KontoId == accountId && k.BenutzerId == user.Id);

            return access?.Zugriffslevel ?? Kontozugriffelevel.Keiner;
        }

        private decimal CalculateCurrentBalance(Guid accountId)
        {
            var account = _db.Bankkonten
                .Include(b => b.Transaktionen)
                .FirstOrDefault(b => b.Id == accountId);

            if (account == null) return 0;

            decimal income = account.Transaktionen
                .Where(t => t.EmpfaengerIBAN == account.IBAN)
                .Sum(t => (decimal?)t.Betrag) ?? 0;

            decimal expenses = account.Transaktionen
                .Where(t => t.AbsenderIBAN == account.IBAN)
                .Sum(t => (decimal?)t.Betrag) ?? 0;

            return income - expenses;
        }

        private List<Transaktion> GetRecentTransactions(Guid accountId, int count)
        {
            return _db.Transaktionen
                .Where(t => t.BankkontoId == accountId)
                .OrderByDescending(t => t.Buchungsdatum)
                .Take(count)
                .ToList();
        }

        private MonthlyStatsVM CalculateMonthlyStats(Guid accountId)
        {
            // TODO: Implementiere monatliche Statistiken
            return new MonthlyStatsVM();
        }

        private List<KontozugriffVM> GetAccountAccess(Guid accountId)
        {
            return _db.Kontozugriffe
                .Where(k => k.KontoId == accountId)
                .Include(k => k.Benutzer)
                .Select(k => new KontozugriffVM
                {
                    UserId = k.BenutzerId,
                    Username = k.Benutzer.Benutzername,
                    AccessLevel = k.Zugriffslevel
                })
                .ToList();
        }

        private SelectList GetAvailableUsersForAccess(Guid accountId)
        {
            var existingUserIds = _db.Kontozugriffe
                .Where(k => k.KontoId == accountId)
                .Select(k => k.BenutzerId);

            var availableUsers = _db.Benutzer
                .Where(b => !existingUserIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Benutzername })
                .ToList();

            return new SelectList(availableUsers, "Id", "Benutzername");
        }

        private SelectList GetCurrencySelectList()
        {
            var currencies = Enum.GetValues(typeof(Waehrung))
                .Cast<Waehrung>()
                .Select(w => new { Value = (int)w, Text = w.ToString() })
                .ToList();

            return new SelectList(currencies, "Value", "Text");
        }

        private SelectList GetAccountTypeSelectList()
        {
            var types = Enum.GetValues(typeof(Kontotyp))
                .Cast<Kontotyp>()
                .Where(k => k != Kontotyp.None)
                .Select(k => new { Value = (int)k, Text = k.ToString() })
                .ToList();

            return new SelectList(types, "Value", "Text");
        }

        private void LogError(string action, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error in {action}: {ex.Message}");
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