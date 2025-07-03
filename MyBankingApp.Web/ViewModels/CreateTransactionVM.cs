using MyBankingApp.Data.Entitites;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyBankingApp.Web.ViewModels
{
    public class CreateTransactionVM
    {
        [Required(ErrorMessage = "Bitte wählen Sie ein Konto.")]
        [Display(Name = "Von Konto")]
        public Guid FromAccountId { get; set; }

        [Required(ErrorMessage = "Bitte geben Sie einen Betrag ein.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Der Betrag muss größer als 0 sein.")]
        [Display(Name = "Betrag")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Bitte geben Sie eine Beschreibung ein.")]
        [StringLength(200, ErrorMessage = "Beschreibung zu lang!")]
        [Display(Name = "Verwendungszweck")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "Empfängername zu lang!")]
        [Display(Name = "Empfänger/Absender")]
        public string CounterParty { get; set; }

        [StringLength(22, ErrorMessage = "IBAN zu lang!")]
        [Display(Name = "Empfänger IBAN")]
        public string CounterPartyIBAN { get; set; }

        [StringLength(50, ErrorMessage = "Kategorie zu lang!")]
        [Display(Name = "Kategorie")]
        public string Category { get; set; }

        [Display(Name = "Auszahlung (sonst Einzahlung)")]
        public bool IsWithdrawal { get; set; }

        // Für die Auswahl verfügbare Konten
        public List<Bankkonto> AvailableAccounts { get; set; }

        public CreateTransactionVM()
        {
            AvailableAccounts = new List<Bankkonto>();
        }
    }
}