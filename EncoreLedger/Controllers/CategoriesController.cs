using EncoreLedger.Data;
using EncoreLedger.Models;
using Microsoft.AspNetCore.Mvc;

namespace EncoreLedger.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Categories.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.DateCreated = DateTime.Now;
                category.DateEdited = DateTime.Now;

                _context.Categories.Add(category);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                // Update the edited timestamp
                category.DateEdited = DateTime.Now;

                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                var categories = _context.Categories
                    .Where(c => selectedIds.Contains(c.IDCategory))
                    .ToList();

                var categoriesWithTransactions = new Dictionary<string, int>();

                foreach (var category in categories)
                {
                    var transactionCount = _context.Transactions
                        .Count(t => t.CategoryID == category.IDCategory);

                    if (transactionCount > 0)
                    {
                        categoriesWithTransactions[category.Name ?? $"Category {category.IDCategory}"] = transactionCount;
                    }
                }

                if (categoriesWithTransactions.Count > 0)
                {
                    var errorMessage = "Cannot delete the following category(s) because they have associated transaction(s):\n";
                    foreach (var item in categoriesWithTransactions)
                    {
                        errorMessage += $"- {item.Key} ({item.Value} transaction{(item.Value > 1 ? "s" : "")})\n";
                    }
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.RemoveRange(categories);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Category(s) deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}