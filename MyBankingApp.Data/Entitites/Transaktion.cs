using MyBankingApp.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public bool IstGutschrift // Wichtig für anzeige
        {
            get
            {
                if (Bankkonto != null && Bankkonto.IBAN == EmpfaengerIBAN) // else muss IBAN = AbsenderIBAn sein, sonst gehört der kontoauszug nicht hier rein
                {
                    return true;
                }
                else return false;
            }
        }
            public Transaktion()
        {
            Id = Guid.NewGuid();
            VerkTransaktionGV = new HashSet<VerkTransaktionGV>();
        }
        
    }
}
