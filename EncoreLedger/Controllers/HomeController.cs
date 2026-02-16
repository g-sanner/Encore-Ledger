using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}