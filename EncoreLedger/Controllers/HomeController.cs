using Microsoft.AspNetCore.Mvc;
using EncoreLedger.Data;

namespace EncoreLedger.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var hasAccounts = _context.Accounts.Any();
            ViewData["HasAccounts"] = hasAccounts;
            return View();
        }
    }
}