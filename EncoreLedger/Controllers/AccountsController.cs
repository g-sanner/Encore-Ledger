using EncoreLedger.Data;
using EncoreLedger.Models;
using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Accounts.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Account account)
        {
            if (ModelState.IsValid)
            {
                _context.Accounts.Add(account);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var account = _context.Accounts.Find(id);
            if (account == null)
                return NotFound();

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Account account)
        {
            if (ModelState.IsValid)
            {
                _context.Accounts.Update(account);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                var accounts = _context.Accounts
                    .Where(a => selectedIds.Contains(a.IDAccount))
                    .ToList();

                _context.Accounts.RemoveRange(accounts);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
