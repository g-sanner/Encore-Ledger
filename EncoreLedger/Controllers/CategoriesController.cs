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

                _context.Categories.RemoveRange(categories);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}