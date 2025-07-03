using System;
using System.Collections.Generic;
using MyBankingApp.Data.Entitites;

namespace MyBankingApp.Web.ViewModels
{
    public class TransactionListVM
    {
        public List<Transaktion> Transactions { get; set; }
        public Guid? AccountId { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public decimal CurrentBalance { get; set; }
        public int TotalTransactions { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public TransactionListVM()
        {
            Transactions = new List<Transaktion>();
            PageSize = 20;
            PageNumber = 1;
        }
    }
}