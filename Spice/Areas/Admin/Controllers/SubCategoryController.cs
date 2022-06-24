using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> Index()
        {
            var subCategories = await _db.SubCategory.Include("Category").ToListAsync();
            return View(subCategories);
        }

        //GET - Create
        public async Task<IActionResult> Create()
        {
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            //if(ModelState.IsValid)
            {
                var subCategory = _db.SubCategory.Include("Category").Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);
                if(subCategory.Count() > 0)
                {
                    //Error
                    StatusMessage = "Error : Sub Category exists under " + subCategory.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                SubCategoryAndCategoryViewModel modelVm = new SubCategoryAndCategoryViewModel()
                {
                    CategoryList = await _db.Category.ToListAsync(),
                    SubCategory = model.SubCategory,
                    SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
                    StatusMessage = StatusMessage
                };
                return View(modelVm);
            }
            //else
              //  return View(model);
        }

        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategories = new List<SubCategory>();
            subCategories = await (from subCategory in _db.SubCategory
                             where subCategory.CategoryId == id
                             select subCategory).ToListAsync();
            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        //GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);

            if (subCategory == null)
                return NotFound();
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel model)
        {
            //if(ModelState.IsValid)
            {
                var subCategory = _db.SubCategory.Include("Category").Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);
                if (subCategory.Count() > 0)
                {
                    //Error
                    StatusMessage = "Error : Sub Category exists under " + subCategory.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    var subcatFromDb = await _db.SubCategory.FindAsync(model.SubCategory.Id);
                    subcatFromDb.Name = model.SubCategory.Name;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                SubCategoryAndCategoryViewModel modelVm = new SubCategoryAndCategoryViewModel()
                {
                    CategoryList = await _db.Category.ToListAsync(),
                    SubCategory = model.SubCategory,
                    SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
                    StatusMessage = StatusMessage
                };
                return View(modelVm);
            }
            //else
            //  return View(model);
        }

        //GET - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);

            if (subCategory == null)
                return NotFound();
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);

            if (subCategory == null)
                return NotFound();
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
                return NotFound();

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);

            if (subCategory == null)
                return NotFound();
            _db.SubCategory.Remove(subCategory);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }


}
