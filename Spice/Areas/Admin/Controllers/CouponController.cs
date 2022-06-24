using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Coupon Coupon { get; set; }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        //GET - Create
        public IActionResult Create()
        {
            return View();
        }

        //POST - Create
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    Coupon.Picture = p1;
                }
                _db.Coupon.Add(Coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
                return View(Coupon);
        }

        //GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            Coupon = await _db.Coupon.FindAsync(id);
            return View(Coupon);
        }

        //POST - Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if (id == null)
                return NotFound();
            if (ModelState.IsValid)
            {
                var couponFromDB = await _db.Coupon.FindAsync(id);
                if (couponFromDB == null)
                    return NotFound();

                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    couponFromDB.Picture = p1;
                }
                couponFromDB.Name = Coupon.Name;
                couponFromDB.Discount = Coupon.Discount;
                couponFromDB.MinimumAmount = Coupon.MinimumAmount;
                couponFromDB.CouponType = Coupon.CouponType;
                couponFromDB.isActive = Coupon.isActive;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            else
                return View();
        }

        //GET - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            Coupon = await _db.Coupon.FindAsync(id);
            return View(Coupon);
        }

        //GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            Coupon = await _db.Coupon.FindAsync(id);
            return View(Coupon);
        }

        //POST - Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (id == null)
                return NotFound();
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
                return NotFound();
            _db.Coupon.Remove(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
