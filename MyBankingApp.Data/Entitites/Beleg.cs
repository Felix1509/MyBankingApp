using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Beleg
    {
        public Guid Id { get; set; }
        public string Bezeichnung { get; set; } // Bezeichnung des Belege
        public virtual ICollection<Geldvorgang> Geldvorgaenge { get; set; } // Liste der Geldvorgänge, die diesem Beleg zugeordnet sind

    }
}
