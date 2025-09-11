using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductManager.Models;
using System.Security.Claims;

namespace ProductManager.Controllers
{
    public class MyOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public MyOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 我的訂單清單
        public async Task<IActionResult> Index(string? Status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // ASP.NET Core Identity 的 UserId
            //var userId = User.FindFirstValue("UserId"); // 自訂的 Claim

            if (!int.TryParse(userId, out var uid))
                return RedirectToAction("Login", "Account");

            var query = _context.Orders.Where(o => o.UserId == uid);


            if (!string.IsNullOrEmpty(Status) && Status != "全部")
                query = query.Where(o => o.Status == Status);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            //提供狀態清單給view
            ViewBag.StatusList = new SelectList(new List<string> { "全部", "待處理", "已出貨", "已完成", "已取消" },
            selectedValue: Status);            

            return View(orders);
        }

        // 我的訂單詳細
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid))
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
