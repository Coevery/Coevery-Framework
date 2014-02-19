using Coevery.Themes;
using System.Web.Mvc;


namespace Coevery.Home.Controllers
{
    [Themed]
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}