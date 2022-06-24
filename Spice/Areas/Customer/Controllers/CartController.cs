using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe.Checkout;
using System.Security.Claims;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrderDetailsCart detailsCart { get; set; }
        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claims.Value);
            if(cart != null)
            {
                detailsCart.ListCart = cart.ToList();
            }

            foreach(var list in detailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = (detailsCart.OrderHeader.OrderTotal) + (list.MenuItem.Price * list.Count);

                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);
                if (list.MenuItem.Description.Length > 100)
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
            }

            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;

            if(HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(detailsCart);
        }
        public async Task<IActionResult> Summary()
        {
            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(c => c.Id == claims.Value);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claims.Value);
            if(cart != null)
            {
                detailsCart.ListCart = cart.ToList();
            }

            foreach(var list in detailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = (detailsCart.OrderHeader.OrderTotal) + (list.MenuItem.Price * list.Count);

            }

            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;
            detailsCart.OrderHeader.PickupName = applicationUser.Name;
            detailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            detailsCart.OrderHeader.PickUpTime = DateTime.Now;


            if(HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(detailsCart);
        }


        [HttpPost,ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            detailsCart.ListCart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == claims.Value).ToListAsync();
            //detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            //Directly approving payment
            detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
            detailsCart.OrderHeader.OrderDate = DateTime.Now;
            detailsCart.OrderHeader.UserId = claims.Value;
            //detailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            detailsCart.OrderHeader.Status = SD.StatusSubmitted;
            detailsCart.OrderHeader.PickUpTime = Convert.ToDateTime(detailsCart.OrderHeader.PickUpTime.ToShortDateString() + " " + detailsCart.OrderHeader.PickUpTime.ToShortTimeString());

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _db.OrderHeader.Add(detailsCart.OrderHeader);
            await _db.SaveChangesAsync();

            detailsCart.OrderHeader.OrderTotalOriginal = 0;


            foreach (var item in detailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails()
                {
                    MenuItemId = (int)item.MenuItemId,
                    OrderId = detailsCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };

                detailsCart.OrderHeader.OrderTotalOriginal += (int)orderDetails.Count * (double)orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);

            }


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotalOriginal;
            }

            detailsCart.OrderHeader.CouponCodeDiscount = detailsCart.OrderHeader.OrderTotalOriginal - detailsCart.OrderHeader.OrderTotal;
            
            


            //Stripe checkout
            //stripe settings
            /*
            var domain = "https://localhost:7111/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },
                LineItems = new List<SessionLineItemOptions>(),

                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={detailsCart.OrderHeader.Id}",
                CancelUrl = domain + $"customer/cart/Index"
            };

            foreach (var item in detailsCart.ListCart)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.MenuItem.Price * 100),
                        Currency = "inr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.MenuItem.Name
                        },

                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            Response.Headers.Add("Location", session.Url);
            */

            _db.ShoppingCart.RemoveRange(detailsCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            // return new StatusCodeResult(303);



            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm","Order",new {id=detailsCart.OrderHeader.Id});
        }

        public IActionResult AddCoupon()
        {
            if(detailsCart.OrderHeader.CouponCode == null)
            {
                detailsCart.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString(SD.ssCouponCode, detailsCart.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            if(cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                var count = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);

            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            var count = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

            return RedirectToAction(nameof(Index));
        }

        
    }
}
