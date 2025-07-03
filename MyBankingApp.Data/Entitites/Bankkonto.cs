using MyBankingApp.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Bankkonto
    {
        public Guid Id { get; set; }
        public string Bezeichnung { get; set; }

        [Required]
        public string IBAN { get; set; }
        public string BIC { get; set; }
        public string Bankname { get; set; }
        public string BLZ { get; set; }
        public string Kontonummer { get; set; }
        public Kontotyp Kontotyp { get; set; } = Kontotyp.Girokonto;
        public Waehrung Waehrung { get; set; } = Waehrung.EUR;
        public string Kontoinhaber { get; set; }
        public decimal AktuellerSaldo { get; set; }

        public virtual ICollection<Transaktion> Transaktionen { get; set; }

        public Bankkonto()
        {
            Id = Guid.NewGuid();
            Transaktionen = new HashSet<Transaktion>();
        }
    }
}
