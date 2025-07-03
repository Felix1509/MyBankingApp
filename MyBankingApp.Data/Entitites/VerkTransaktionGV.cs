using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{ 
    // Eine Verknüpfnungstabelle, um n zu n beziehungen zwischen Geldvorgaengen und Transaktionen zu ermöglichen
    // Ein Geldvorgang kann von mehreren Transaktionen gedeckt sein. Eine Transaktion kann mehrere Geldvorgänge decken.
    public class VerkTransaktionGV
    {
        [Key]
        public Guid GeldvorgangId { get; set; } // ID des zugehörigen Belege
        public virtual Geldvorgang Geldvorgang { get; set; } // Zuordnung zum Beleg-Objekt
        [Key]
        public Guid TransaktionId { get; set; } // ID der zugehörigen Transaktion
        public virtual Transaktion Transaktion { get; set; } // Zuordnung zum Transaktion-Objekt
    }
}
