using System;
using System.Collections.Generic;

namespace MyBankingApp.Web.ViewModels
{
    public class TransactionsVM
    {
        public List<AccountSelectionVM> AvailableAccounts { get; set; }

        public TransactionsVM()
        {
            AvailableAccounts = new List<AccountSelectionVM>();
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