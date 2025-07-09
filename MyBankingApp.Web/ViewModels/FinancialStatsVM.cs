using System.Collections.Generic;

namespace MyBankingApp.Web.ViewModels
{
    public class FinancialStatsVM
    {
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal SavingsRate { get; set; }
        public Dictionary<string, decimal> ExpensesByCategory { get; set; }
        public List<decimal> Last6MonthsBalance { get; set; }

        public FinancialStatsVM()
        {
            ExpensesByCategory = new Dictionary<string, decimal>();
            Last6MonthsBalance = new List<decimal>();
        }
    }
}