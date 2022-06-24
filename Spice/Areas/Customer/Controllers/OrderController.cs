using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using System.Security.Claims;
using System.Text;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 2;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }


        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(m => m.ApplicationUser).FirstOrDefaultAsync(m => m.Id == id && m.UserId == claims.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };
            return View(orderDetailsViewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Include(u => u.ApplicationUser).Where(u => u.UserId == claim.Value).ToListAsync();
            foreach (OrderHeader item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            };

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders
                                 .OrderByDescending(p => p.OrderHeader.Id)
                                 .Skip((productPage - 1) * PageSize)
                                 .Take(PageSize)
                                 .ToList();
            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                UrlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            return View(orderListVM);
        }

        [Authorize(Roles =SD.KitchenUser + "," +SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();

            List<OrderHeader> OrderHeaderList = await _db.OrderHeader
                                                .Where(o=>o.Status == SD.StatusSubmitted || o.Status == SD.StatusInProcess)
                                                .OrderByDescending(o=>o.PickUpTime).ToListAsync();

            foreach (OrderHeader item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderDetailsVM.Add(individual);
            };

            return View(orderDetailsVM.OrderBy(o=>o.OrderHeader.PickUpTime));
        }


        public async Task<IActionResult> GetOrderDetails(int Id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(m => m.Id == Id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == Id).ToListAsync()
            };

            orderDetailsViewModel.OrderHeader.ApplicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == orderDetailsViewModel.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);
        }

        public async Task<IActionResult> GetOrderStatus(int Id)
        {
            var orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o=>o.Id == Id);
            return PartialView("_OrderStatus", orderHeader.Status);
        }

        [Authorize(Roles = SD.KitchenUser + ","+SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();

            //Email logic to notify that order is ready for pickup
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchEmail = null, string searchName = null, string searchPhone = null)
        {
            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param  = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&searchName=");
            if(searchName !=null)
            {
                param.Append(searchName);
            }
            param.Append("&searchPhone=");
            if (searchPhone != null)
            {
                param.Append(searchPhone);
            }
            param.Append("&searchEmail=");
            if (searchEmail != null)
            {
                param.Append(searchEmail);
            }
            List<OrderHeader> OrderHeaderList = new List<OrderHeader>();

            if (searchEmail != null || searchPhone != null || searchName != null)
            {
                if (searchName != null)
                {
                    OrderHeaderList = await _db.OrderHeader.Include(u => u.ApplicationUser)
                                        .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower()))
                                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else if (searchEmail != null)
                {
                    var user = await _db.ApplicationUser.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower())).FirstOrDefaultAsync();
                    OrderHeaderList = await _db.OrderHeader.Include(u => u.ApplicationUser)
                                        .Where(o => o.UserId == user.Id)
                                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else if (searchPhone != null)
                {
                    OrderHeaderList = await _db.OrderHeader.Include(u => u.ApplicationUser)
                                        .Where(u => u.PhoneNumber.Contains(searchPhone))
                                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
            }
            else
            {
                OrderHeaderList = await _db.OrderHeader.Include(u => u.ApplicationUser).Where(u => u.Status == SD.StatusReady).ToListAsync();
            }
            foreach (OrderHeader item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            };
            

            

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders
                                 .OrderByDescending(p => p.OrderHeader.Id)
                                 .Skip((productPage - 1) * PageSize)
                                 .Take(PageSize)
                                 .ToList();
            orderListVM.PagingInfo = new PagingInfo()
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                UrlParam = param.ToString()
            };

            return View(orderListVM);
        }

        [Authorize(Roles =SD.ManagerUser +","+SD.FrontDeskUser)]
        [HttpPost,ActionName("OrderPickup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderPickupPost(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusCompleted;
            await _db.SaveChangesAsync();
            return RedirectToAction("OrderPickup", "Order");
        }
    }
}
