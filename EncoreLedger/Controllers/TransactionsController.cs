using EncoreLedger.Models;
using EncoreLedger.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EncoreLedger.Models.ViewModels;

namespace EncoreLedger.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            bool displayAccountName = false,
            string sortColumn = "Date",
            bool ascending = true)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsQueryable();

            // Sorting
            query = sortColumn switch
            {
                "Category" => ascending
                    ? query.OrderBy(t => t.Category.Name)
                    : query.OrderByDescending(t => t.Category.Name),

                "Amount" => ascending
                    ? query.OrderBy(t => t.Amount)
                    : query.OrderByDescending(t => t.Amount),

                _ => ascending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate)
            };

            var vm = new TransactionIndexViewModel
            {
                Transactions = await query.ToListAsync(),
                DisplayAccountName = displayAccountName,
                SortColumn = sortColumn,
                Ascending = ascending
            };

            return View(vm);
        }


        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Transaction transaction)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(transaction);
            }

            _context.Add(transaction);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        /* TODO: Replace with ViewModel as time goes on? */
        private void LoadDropdowns()
        {
            ViewBag.Accounts = new SelectList(
                _context.Accounts.ToList(),
                "ID", "Name"
            );

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "ID", "Name"
            );
        }
        public async Task<IActionResult> Filter(
            bool displayAccountName,
            string sortColumn,
            bool ascending)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsQueryable();

            query = sortColumn switch
            {
                "Category" => ascending
                    ? query.OrderBy(t => t.Category.Name)
                    : query.OrderByDescending(t => t.Category.Name),

                "Amount" => ascending
                    ? query.OrderBy(t => t.Amount)
                    : query.OrderByDescending(t => t.Amount),

                _ => ascending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate)
            };

            var vm = new TransactionIndexViewModel
            {
                Transactions = await query.ToListAsync(),
                DisplayAccountName = displayAccountName,
                SortColumn = sortColumn,
                Ascending = ascending
            };

            return PartialView("_TransactionTable", vm);
        }

    }
}