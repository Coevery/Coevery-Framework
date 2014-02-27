using Coevery.Themes;
using System.Web.Mvc;


namespace Coevery.Core.Common.Controllers {
    [Themed]
    public class HomeController : Controller {
        public ActionResult Index() {
            return View();
        }
    }
}