using EncoreLedger.Data;
using EncoreLedger.Models;
using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static readonly HashSet<string> ValidAccountTypes = new() { "Savings", "Checkings", "Joint", "Other" };

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
            if (!ValidAccountTypes.Contains(account.AccountType ?? ""))
            {
                ModelState.AddModelError(nameof(account.AccountType), "Invalid account type selected.");
            }

            if (ModelState.IsValid)
            {
                account.DateCreated = DateTime.Now;
                account.DateEdited = DateTime.Now;

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
            if (!ValidAccountTypes.Contains(account.AccountType ?? ""))
            {
                ModelState.AddModelError(nameof(account.AccountType), "Invalid account type selected.");
            }

            if (ModelState.IsValid)
            {
                // Update the edited timestamp
                account.DateEdited = DateTime.Now;

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

                var accountsWithTransactions = new Dictionary<string, int>();

                foreach (var account in accounts)
                {
                    var transactionCount = _context.Transactions
                        .Count(t => t.AccountID == account.IDAccount);

                    if (transactionCount > 0)
                    {
                        accountsWithTransactions[account.AccountName ?? $"Account {account.IDAccount}"] = transactionCount;
                    }
                }

                if (accountsWithTransactions.Count > 0)
                {
                    var errorMessage = "Cannot delete the following account(s) because they have associated transaction(s):\n";
                    foreach (var item in accountsWithTransactions)
                    {
                        errorMessage += $"- {item.Key} ({item.Value} transaction{(item.Value > 1 ? "s" : "")})\n";
                    }
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(Index));
                }

                _context.Accounts.RemoveRange(accounts);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Account(s) deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
