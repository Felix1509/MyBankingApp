using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Benutzer
    {
        public Guid Id { get; set; }
        public string Benutzername { get; set; } // Eindeutiger Benutzername
        public string Passwort { get; set; } // Gespeichertes Passwort -> Übung

        public virtual ICollection<Kontozugriff> Kontozugriffe { get; set; } // Liste der Kontozugriffe des Benutzers

    }
}
