using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Common.Enums
{
    public enum Kontozugriffelevel
    {
        Keiner = 0,             // Kein Zugriff auf das Konto
        Anzeigen = 1,           // Anzeige der Kontodaten (keine Salden etc.)
        NurLesen = 2,           // Wie 1   + Leserechte für Transaktionen
        LesenVoll = 3,          // Wie 1-2 + Erstellen u. Verändern von Verknüpfungen zwischen Transaktion und Geldvorgang
        Zahlungsfunktion = 4,   // Wie 1-3 + Erstellen von Zahlungsanweisungen
        Admin = 5               // Wie 1-4 + Zugriff auf Konto für andere Benutzer freigeben
    }
}
