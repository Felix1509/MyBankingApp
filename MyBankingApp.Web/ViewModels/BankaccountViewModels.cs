using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyBankingApp.Common.Enums;
using MyBankingApp.Data.Entitites;

namespace MyBankingApp.Web.ViewModels
{
    public class BankAccountDetailsVM
    {
        public Bankkonto Account { get; set; }
        public MonthlyStatsVM MonthlyStats { get; set; }
        public List<Transaktion> RecentTransactions { get; set; }
        public Kontozugriffelevel AccessLevel { get; set; }
    }

    public class MonthlyStatsVM
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetChange { get; set; }
        public int TransactionCount { get; set; }
        public Dictionary<string, decimal> TopCategories { get; set; }

        public MonthlyStatsVM()
        {
            TopCategories = new Dictionary<string, decimal>();
        }
    }

    public class CreateBankAccountVM
    {
        [Required(ErrorMessage = "IBAN ist erforderlich")]
        [Display(Name = "IBAN")]
        [RegularExpression(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$", ErrorMessage = "Ungültige IBAN")]
        public string IBAN { get; set; }

        [Display(Name = "BIC")]
        public string BIC { get; set; }

        [Required(ErrorMessage = "Bankname ist erforderlich")]
        [Display(Name = "Bank")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Kontoinhaber ist erforderlich")]
        [Display(Name = "Kontoinhaber")]
        public string AccountHolder { get; set; }

        [Display(Name = "Kontotyp")]
        public Kontotyp AccountType { get; set; }

        [Display(Name = "Währung")]
        public Waehrung Currency { get; set; }

        [Display(Name = "Beschreibung")]
        public string Description { get; set; }

        // Für Dropdowns
        public System.Web.Mvc.SelectList AvailableCurrencies { get; set; }
        public System.Web.Mvc.SelectList AvailableAccountTypes { get; set; }
    }

    public class EditBankAccountVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Beschreibung")]
        public string Description { get; set; }

        [Display(Name = "Kontoinhaber")]
        public string AccountHolder { get; set; }

        // Für Dropdowns
        public System.Web.Mvc.SelectList AvailableCurrencies { get; set; }
        public System.Web.Mvc.SelectList AvailableAccountTypes { get; set; }
    }

    public class ManageAccountAccessVM
    {
        public Bankkonto Account { get; set; }
        public List<KontozugriffVM> CurrentAccess { get; set; }
        public System.Web.Mvc.SelectList AvailableUsers { get; set; }
    }

    public class KontozugriffVM
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public Kontozugriffelevel AccessLevel { get; set; }
    }
}