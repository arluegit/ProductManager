using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Index()
        {
            // 取得登入使用者的 Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            // 撈出這個使用者的訂單
            var orders = await _context.Orders
                .Where(o => o.UserId == int.Parse(userId)) // 確認 Order 有 UserId 欄位
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 我的訂單詳細
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == int.Parse(userId));

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
