using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class TransactionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}