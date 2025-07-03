using MyBankingApp.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyBankingApp.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to DevExpress Extensions for ASP.NET MVC!";

            return View();
        }
        [HttpGet]
        public ActionResult Contact()
        {
            return View(new ContactVM());

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(ContactVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Hier würden Sie die Nachricht verarbeiten
                    // z.B. E-Mail senden, in Datenbank speichern, etc.

                    // Bei AJAX-Aufruf JSON zurückgeben
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Nachricht erfolgreich gesendet!" });
                    }

                    // Bei normalem POST-Request Redirect oder View mit Erfolgsmodell
                    TempData["SuccessMessage"] = "Vielen Dank für Ihre Nachricht! Wir werden uns schnellstmöglich bei Ihnen melden.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Fehler-Logging hier einfügen
                    ModelState.AddModelError("", "Ein Fehler ist beim Senden der Nachricht aufgetreten. Bitte versuchen Sie es erneut.");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Fehler beim Senden der Nachricht." });
                    }
                }
            }

            // Bei Validierungsfehlern oder anderen Fehlern
            if (Request.IsAjaxRequest())
            {
                // Validierungsfehler für AJAX sammeln
                var errors = new System.Collections.Generic.List<string>();
                foreach (var modelError in ModelState.Values)
                {
                    foreach (var error in modelError.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                return Json(new { success = false, errors = errors });
            }

            // Normale View mit Fehlern zurückgeben
            return View(model);
        }
    }
}