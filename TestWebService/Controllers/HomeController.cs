using System.Web.Mvc;

namespace TestWebService.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return RedirectToAction("ResponseTime", "Transaction");
		}
	}
}
