using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyBankingApp.Common.Enums;

namespace MyBankingApp.Web.ViewModels
{
    public class TransactionFilterVM
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string SearchText { get; set; }
        public string Category { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        public List<Guid> SelectedAccountIds { get; set; }

        public TransactionFilterVM()
        {
            SelectedAccountIds = new List<Guid>();
        }
    }

    public class TransactionSearchVM
    {
        [Display(Name = "Suchbegriff")]
        public string SearchTerm { get; set; }

        [Display(Name = "Von Datum")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Bis Datum")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Mindestbetrag")]
        public decimal? MinAmount { get; set; }

        [Display(Name = "Höchstbetrag")]
        public decimal? MaxAmount { get; set; }

        [Display(Name = "Kategorie")]
        public string Category { get; set; }

        public bool IncludeCredits { get; set; } = true;
        public bool IncludeDebits { get; set; } = true;
    }

    public class CreateTransactionVM
    {
        [Required(ErrorMessage = "Bitte wählen Sie ein Absenderkonto")]
        [Display(Name = "Von Konto")]
        public Guid FromAccountId { get; set; }

        [Required(ErrorMessage = "Empfänger-IBAN ist erforderlich")]
        [Display(Name = "Empfänger IBAN")]
        [RegularExpression(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$", ErrorMessage = "Ungültige IBAN")]
        public string RecipientIBAN { get; set; }

        [Required(ErrorMessage = "Empfängername ist erforderlich")]
        [Display(Name = "Empfängername")]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = "Betrag ist erforderlich")]
        [Display(Name = "Betrag")]
        [Range(0.01, 999999.99, ErrorMessage = "Betrag muss zwischen 0,01 und 999.999,99 liegen")]
        public decimal Amount { get; set; }

        [Display(Name = "Währung")]
        public Waehrung Currency { get; set; } = Waehrung.EUR;

        [Required(ErrorMessage = "Verwendungszweck ist erforderlich")]
        [Display(Name = "Verwendungszweck")]
        [StringLength(140, ErrorMessage = "Verwendungszweck darf maximal 140 Zeichen lang sein")]
        public string Purpose { get; set; }

        [Display(Name = "Buchungsdatum")]
        public DateTime TransactionDate { get; set; }

        [Display(Name = "Valutadatum")]
        public DateTime ValueDate { get; set; }

        [Display(Name = "Kategorie")]
        public string Category { get; set; }

        // Für Dropdown
        public System.Web.Mvc.SelectList AvailableAccounts { get; set; }
    }
}