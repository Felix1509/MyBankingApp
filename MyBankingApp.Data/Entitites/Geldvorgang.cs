using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Geldvorgang
    {
        public Guid Id { get; set; } // Eindeutige ID des Geldvorgaenge
        public DateTime? Datum { get; set; } // Datum des Geldvorgaenge
        public string Bezeichnung { get; set; } // Bezeichnung des Geldvorgaenge

        public Guid? BelegId { get; set; } // ID des zugehörigen Belege (optional)
        public virtual Beleg Beleg { get; set; } // Zuordnung zum Beleg-Objekt (optional)

        public virtual ICollection<VerkTransaktionGV> VerkTransaktionenGV { get; set; } // Liste der Verknüpfungen zu Transaktionen

        public Geldvorgang()
        {
            Id = Guid.NewGuid();
            VerkTransaktionenGV = new HashSet<VerkTransaktionGV>();
        }

    }
}
