using System.Collections.Generic;
using MyBankingApp.Data.Context;
using MyBankingApp.Data.Entitites;
using MyBankingApp.Data.Repositories;

namespace MyBankingApp.Web.ViewModels
{
    public class DashboardVM
    {
        public List<Bankkonto> BankAccounts { get; set; }
        public List<Transaktion> RecentTransactions { get; set; }
        public decimal TotalBalance { get; set; }
        public int TotalAccounts { get; set; }
        public int TransactionsThisMonth { get; set; }
        public string WelcomeMessage { get; set; }
       
        public DashboardVM()
        {
            BankAccounts = new List<Bankkonto>();
            RecentTransactions = new List<Transaktion>();
        }
    }
}