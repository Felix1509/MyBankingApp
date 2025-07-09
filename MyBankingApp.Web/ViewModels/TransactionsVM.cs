using System;
using System.Collections.Generic;

namespace MyBankingApp.Web.ViewModels
{
    public class TransactionsVM
    {
        public List<AccountSelectionVM> AvailableAccounts { get; set; }
        public TransactionFilterVM Filter { get; set; }  // DIESE ZEILE FEHLT BEI IHNEN!

        public TransactionsVM()
        {
            AvailableAccounts = new List<AccountSelectionVM>();
            Filter = new TransactionFilterVM();  // UND DIESE ZEILE AUCH!
        }
    }

    public class AccountSelectionVM
    {
        public Guid KontoId { get; set; }
        public string Bezeichnung { get; set; }
        public string IBAN { get; set; }
        public decimal Saldo { get; set; }
        public bool IsSelected { get; set; }
    }
}