using System;
using System.ComponentModel.DataAnnotations;

namespace MyBankingApp.Web.ViewModels
{
    public class ContactVM
    {
        [Required(ErrorMessage = "Bitte geben Sie Ihren Namen an.")]
        [StringLength(50, ErrorMessage = "Name zu lang!")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Bitte geben Sie eine E-Mail-Adresse an.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail erkannt!")]
        public string EmailAddress { get; set; }
        [Required(ErrorMessage = "Bitte geben Sie eine Telefon-Nummer an.")]
        [Phone(ErrorMessage = "Ungültige Telefonnummer erkannt!")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Bitte geben Sie eine Nachricht ein.")]
        [StringLength(200, ErrorMessage = "Nachricht zu lang!")]
        public string Message { get; set; }
    }
}