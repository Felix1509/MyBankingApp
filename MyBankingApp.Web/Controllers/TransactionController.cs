using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Web.ViewModels;
using System.Data.Entity;

namespace MyBankingApp.Web.Controllers
{
    /// <summary>
    /// Controller für Transaktionsverwaltung
    /// </summary>
    public class TransactionController : Controller
    {
        private readonly BankingDbContext _db;
        private readonly string _defaultUsername = "Felix"; // TODO: Durch echte Authentication ersetzen

        #region Constructor

        public TransactionController(BankingDbContext context)
        {
            _db = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region List & Display Actions

        /// <summary>
        /// Zeigt die Hauptübersicht aller Transaktionen
        /// </summary>
        public ActionResult Index(TransactionFilterVM filter = null)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Account");

                var viewModel = new TransactionsVM
                {
                    AvailableAccounts = GetUserAccountsForSelection(user.Id),
                    Filter = filter ?? new TransactionFilterVM()
                };

                // Setze Session für Grid-Callbacks
                SetSelectedAccountsInSession(viewModel.AvailableAccounts);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("Transaction.Index", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Lädt das Transaktions-Grid (Partial View)
        /// </summary>
        public ActionResult TransactionGrid()
        {
            try
            {
                var transactions = GetFilteredTransactions();
                return PartialView("_TransactionGrid", transactions);
            }
            catch (Exception ex)
            {
                LogError("Transaction.TransactionGrid", ex);
                return PartialView("_TransactionGrid", new List<Transaktion>());
            }
        }

        /// <summary>
        /// DevExpress Grid Callback
        /// </summary>
        public ActionResult TransactionGridCallback()
        {
            return TransactionGrid();
        }

        /// <summary>
        /// Zeigt Details einer einzelnen Transaktion
        /// </summary>
        public ActionResult Details(Guid id)
        {
            try
            {
                var transaction = _db.Transaktionen
                    .Include(t => t.Bankkonto)
                    .Include(t => t.VerkTransaktionGV.Select(v => v.Geldvorgang))
                    .FirstOrDefault(t => t.Id == id);

                if (transaction == null)
                {
                    return HttpNotFound();
                }

                // Prüfe Zugriffsberechtigung
                if (!UserHasAccessToTransaction(transaction))
                {
                    return new HttpUnauthorizedResult();
                }

                return View(transaction);
            }
            catch (Exception ex)
            {
                LogError("Transaction.Details", ex);
                return View("Error");
            }
        }

        #endregion

        #region Filter & Search Actions

        /// <summary>
        /// Aktualisiert die Kontoauswahl für Filter
        /// </summary>
        [HttpPost]
        public ActionResult UpdateAccountFilter(List<Guid> selectedAccountIds)
        {
            try
            {
                Session["SelectedAccountIds"] = selectedAccountIds ?? new List<Guid>();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                LogError("Transaction.UpdateAccountFilter", ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Erweiterte Suche für Transaktionen
        /// </summary>
        public ActionResult Search(TransactionSearchVM searchModel)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Account");

                var results = SearchTransactions(searchModel, user.Id);
                ViewBag.SearchModel = searchModel;

                return View(results);
            }
            catch (Exception ex)
            {
                LogError("Transaction.Search", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Autocomplete für Empfänger/Absender
        /// </summary>
        [HttpGet]
        public ActionResult GetPayees(string term)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return Json(new List<string>(), JsonRequestBehavior.AllowGet);

                var payees = GetUniquePayees(user.Id, term);
                return Json(payees, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogError("Transaction.GetPayees", ex);
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Create & Edit Actions

        /// <summary>
        /// Zeigt Formular für neue Überweisung
        /// </summary>
        public ActionResult Create()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null) return RedirectToAction("Login", "Account");

                var viewModel = new CreateTransactionVM
                {
                    AvailableAccounts = GetUserAccountsForTransfer(user.Id),
                    TransactionDate = DateTime.Today,
                    ValueDate = DateTime.Today
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError("Transaction.Create", ex);
                return View("Error");
            }
        }

        /// <summary>
        /// Verarbeitet neue Überweisung
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateTransactionVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableAccounts = GetUserAccountsForTransfer(GetCurrentUser().Id);
                    return View(model);
                }

                // Hole das Absender-Konto
                var senderAccount = _db.Bankkonten
                    .Include(b => b.Transaktionen)
                    .FirstOrDefault(b => b.Id == model.FromAccountId);

                if (senderAccount == null)
                {
                    ModelState.AddModelError("", "Absenderkonto nicht gefunden.");
                    model.AvailableAccounts = GetUserAccountsForTransfer(GetCurrentUser().Id);
                    return View(model);
                }

                // Prüfe Kontodeckung
                decimal senderBalance = CalculateAccountBalance(senderAccount);
                if (senderBalance < model.Amount)
                {
                    ModelState.AddModelError("Amount", "Kontodeckung nicht ausreichend. Verfügbar: " + senderBalance.ToString("C"));
                    model.AvailableAccounts = GetUserAccountsForTransfer(GetCurrentUser().Id);
                    return View(model);
                }

                // Erstelle neue Transaktion
                var transaction = new Transaktion
                {
                    Id = Guid.NewGuid(),
                    Buchungsdatum = model.TransactionDate,
                    ValutaDatum = model.ValueDate,
                    Betrag = model.Amount,
                    Waehrung = model.Currency,
                    EmpfaengerName = model.RecipientName,
                    EmpfaengerIBAN = model.RecipientIBAN,
                    AbsenderName = senderAccount.Kontoinhaber,
                    AbsenderIBAN = senderAccount.IBAN,
                    Verwendungszweck = model.Purpose,
                    Kategorie = model.Category,
                    BankkontoId = senderAccount.Id,
                    Bankkonto = senderAccount
                };

                _db.Transaktionen.Add(transaction);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Überweisung wurde erfolgreich ausgeführt.";
                return RedirectToAction("Details", new { id = transaction.Id });
            }
            catch (Exception ex)
            {
                LogError("Transaction.Create", ex);
                ModelState.AddModelError("", "Fehler bei der Überweisung: " + ex.Message);
                model.AvailableAccounts = GetUserAccountsForTransfer(GetCurrentUser().Id);
                return View(model);
            }
        }

        #endregion

        #region Export Actions

        /// <summary>
        /// Exportiert Transaktionen als Excel
        /// </summary>
        public ActionResult ExportExcel(TransactionFilterVM filter)
        {
            try
            {
                var transactions = GetFilteredTransactions(filter);

                // TODO: Implementiere Excel-Export mit EPPlus oder ähnlichem
                // return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                //             $"Transaktionen_{DateTime.Now:yyyyMMdd}.xlsx");

                TempData["InfoMessage"] = "Excel-Export wird noch implementiert.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LogError("Transaction.ExportExcel", ex);
                TempData["ErrorMessage"] = "Fehler beim Excel-Export.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exportiert Transaktionen als CSV
        /// </summary>
        public ActionResult ExportCsv(TransactionFilterVM filter)
        {
            try
            {
                var transactions = GetFilteredTransactions(filter);
                var csv = GenerateCsv(transactions);

                return File(
                    System.Text.Encoding.UTF8.GetBytes(csv),
                    "text/csv",
                    $"Transaktionen_{DateTime.Now:yyyyMMdd}.csv"
                );
            }
            catch (Exception ex)
            {
                LogError("Transaction.ExportCsv", ex);
                TempData["ErrorMessage"] = "Fehler beim CSV-Export.";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Category Management

        /// <summary>
        /// Kategorisiert Transaktionen automatisch
        /// </summary>
        [HttpPost]
        public ActionResult AutoCategorize(List<Guid> transactionIds)
        {
            try
            {
                // TODO: Implementiere Auto-Kategorisierung basierend auf Regeln
                var categorized = 0;

                return Json(new
                {
                    success = true,
                    message = $"{categorized} Transaktionen wurden kategorisiert."
                });
            }
            catch (Exception ex)
            {
                LogError("Transaction.AutoCategorize", ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Private Helper Methods

        private Benutzer GetCurrentUser()
        {
            return _db.Benutzer.FirstOrDefault(b => b.Benutzername == _defaultUsername);
        }

        private List<AccountSelectionVM> GetUserAccountsForSelection(Guid userId)
        {
            var accounts = _db.Kontozugriffe
                .Where(k => k.BenutzerId == userId &&
                       k.Zugriffslevel >= Common.Enums.Kontozugriffelevel.Anzeigen)
                .Include(k => k.Konto)
                .Include(k => k.Konto.Transaktionen)
                .ToList();

            var result = new List<AccountSelectionVM>();

            foreach (var zugriff in accounts)
            {
                var konto = zugriff.Konto;

                // Saldo berechnen
                decimal einnahmen = konto.Transaktionen
                    .Where(t => t.EmpfaengerIBAN == konto.IBAN)
                    .Sum(t => (decimal?)t.Betrag) ?? 0;

                decimal ausgaben = konto.Transaktionen
                    .Where(t => t.AbsenderIBAN == konto.IBAN)
                    .Sum(t => (decimal?)t.Betrag) ?? 0;

                var saldo = einnahmen - ausgaben;

                result.Add(new AccountSelectionVM
                {
                    KontoId = konto.Id,
                    Bezeichnung = konto.Bankname + " - " + konto.Kontoinhaber,
                    IBAN = konto.IBAN,
                    Saldo = saldo,
                    IsSelected = true
                });
            }

            return result;
        }

        private void SetSelectedAccountsInSession(List<AccountSelectionVM> accounts)
        {
            Session["SelectedAccountIds"] = accounts
                .Where(a => a.IsSelected)
                .Select(a => a.KontoId)
                .ToList();
        }

        private List<Transaktion> GetFilteredTransactions(TransactionFilterVM filter = null)
        {
            var selectedAccountIds = Session["SelectedAccountIds"] as List<Guid> ?? new List<Guid>();

            if (!selectedAccountIds.Any())
            {
                var user = GetCurrentUser();
                if (user != null)
                {
                    selectedAccountIds = _db.Kontozugriffe
                        .Where(k => k.BenutzerId == user.Id)
                        .Select(k => k.KontoId)
                        .ToList();
                }
            }

            var query = _db.Transaktionen
                .Where(t => selectedAccountIds.Contains(t.BankkontoId))
                .Include(t => t.Bankkonto);

            // Apply additional filters if provided
            if (filter != null)
            {
                if (filter.DateFrom.HasValue)
                    query = query.Where(t => t.Buchungsdatum >= filter.DateFrom.Value);

                if (filter.DateTo.HasValue)
                    query = query.Where(t => t.Buchungsdatum <= filter.DateTo.Value);

                if (!string.IsNullOrEmpty(filter.SearchText))
                    query = query.Where(t => t.Verwendungszweck.Contains(filter.SearchText) ||
                                        t.EmpfaengerName.Contains(filter.SearchText) ||
                                        t.AbsenderName.Contains(filter.SearchText));

                if (!string.IsNullOrEmpty(filter.Category))
                    query = query.Where(t => t.Kategorie == filter.Category);
            }

            return query.OrderByDescending(t => t.Buchungsdatum).ToList();
        }

        private bool UserHasAccessToTransaction(Transaktion transaction)
        {
            var user = GetCurrentUser();
            if (user == null) return false;

            return _db.Kontozugriffe.Any(k =>
                k.BenutzerId == user.Id &&
                k.KontoId == transaction.BankkontoId &&
                k.Zugriffslevel >= Common.Enums.Kontozugriffelevel.Anzeigen);
        }

        private IQueryable<Transaktion> SearchTransactions(TransactionSearchVM searchModel, Guid userId)
        {
            // TODO: Implementiere erweiterte Suche
            return _db.Transaktionen.Take(0); // Placeholder
        }

        private List<string> GetUniquePayees(Guid userId, string term)
        {
            var userAccountIds = _db.Kontozugriffe
                .Where(k => k.BenutzerId == userId)
                .Select(k => k.KontoId);

            var payees = _db.Transaktionen
                .Where(t => userAccountIds.Contains(t.BankkontoId))
                .Where(t => t.EmpfaengerName.Contains(term) || t.AbsenderName.Contains(term))
                .Select(t => t.EmpfaengerName)
                .Union(_db.Transaktionen
                    .Where(t => userAccountIds.Contains(t.BankkontoId))
                    .Select(t => t.AbsenderName))
                .Distinct()
                .Where(p => p.Contains(term))
                .Take(10)
                .ToList();

            return payees;
        }

        private SelectList GetUserAccountsForTransfer(Guid userId)
        {
            var accounts = _db.Kontozugriffe
                .Where(k => k.BenutzerId == userId &&
                       k.Zugriffslevel >= Common.Enums.Kontozugriffelevel.Zahlungsfunktion)
                .Include(k => k.Konto)
                .Select(k => new
                {
                    k.KontoId,
                    Display = k.Konto.Bankname + " - " + k.Konto.IBAN + " (Saldo: " + k.Konto.AktuellerSaldo + " EUR)"
                })
                .ToList();

            return new SelectList(accounts, "KontoId", "Display");
        }

        private string GenerateCsv(List<Transaktion> transactions)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Datum;Valuta;Betrag;Währung;Empfänger;Absender;Verwendungszweck;Kategorie;Bank");

            foreach (var t in transactions)
            {
                csv.AppendLine($"{t.Buchungsdatum:dd.MM.yyyy};{t.ValutaDatum:dd.MM.yyyy};" +
                             $"{t.Betrag};{t.Waehrung};{t.EmpfaengerName};{t.AbsenderName};" +
                             $"\"{t.Verwendungszweck}\";{t.Kategorie};{t.Bankkonto?.Bankname}");
            }

            return csv.ToString();
        }

        private decimal CalculateAccountBalance(Bankkonto account)
        {
            decimal income = account.Transaktionen
                .Where(t => t.EmpfaengerIBAN == account.IBAN)
                .Sum(t => (decimal?)t.Betrag) ?? 0;

            decimal expenses = account.Transaktionen
                .Where(t => t.AbsenderIBAN == account.IBAN)
                .Sum(t => (decimal?)t.Betrag) ?? 0;

            return income - expenses;
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