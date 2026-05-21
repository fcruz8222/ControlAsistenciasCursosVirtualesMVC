using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Session["USER"] == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }
        public ActionResult SinAcceso()
        {
            return View();
        }
    }
}