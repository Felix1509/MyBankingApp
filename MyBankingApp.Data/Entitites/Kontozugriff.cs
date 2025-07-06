using MyBankingApp.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBankingApp.Data.Entitites
{
    public class Kontozugriff
    {
        [Key]
        public Guid KontoId { get; set; }
        public virtual Bankkonto Konto { get; set; }
        [Key]
        public Guid BenutzerId { get; set; }
        public virtual Benutzer Benutzer { get; set; }
        [Required]
        public Kontozugriffelevel Zugriffslevel { get; set; }
    }
}
