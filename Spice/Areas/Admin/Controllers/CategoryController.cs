using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles =SD.ManagerUser)]
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET   
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }

        //GET - Create
        public IActionResult Create()
        {
            return View();
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if(ModelState.IsValid)
            {
                _db.Category.Add(category);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(category);
            }
        }

        //GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            else
                return View(category);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if(ModelState.IsValid)
            {
                _db.Category.Update(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(category);
            }
        }

        //GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            else
                return View(category);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoryFromDb = await _db.Category.FindAsync(id);
            if (categoryFromDb == null)
                return NotFound();
            _db.Category.Remove(categoryFromDb);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            else
                return View(category);
        }
    }
}
