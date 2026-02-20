using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}