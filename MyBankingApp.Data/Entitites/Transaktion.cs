using MyBankingApp.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Transaktion
    {
        public Guid Id { get; set; }
        public DateTime Buchungsdatum { get; set; }
        public DateTime ValutaDatum { get; set; }
        public decimal Betrag { get; set; }
        public Waehrung Waehrung { get; set; }
        public string EmpfaengerName { get; set; }
        public string EmpfaengerIBAN { get; set; }
        public string AbsenderName { get; set; }
        public string AbsenderIBAN { get; set; }
        public string Verwendungszweck { get; set; }
        public string Kategorie { get; set; }
        public ICollection<VerkTransaktionGV> VerkTransaktionGV { get; set; }
        public Guid BankkontoId { get; set; } // Foreign key for the associated Bankkonto
        public virtual Bankkonto Bankkonto { get; set; } // Navigation property to the associated Bankkonto

        public Transaktion()
        {
            Id = Guid.NewGuid();
            VerkTransaktionGV = new HashSet<VerkTransaktionGV>();
        }
    }
}
