using Microsoft.AspNetCore.Mvc;
using EncoreLedger.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EncoreLedger.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Accounts = new SelectList(_context.Accounts, "IDAccount", "AccountName");

            return View();
        }
    }
}
