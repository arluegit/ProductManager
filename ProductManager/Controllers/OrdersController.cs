using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManager.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProductManager.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 訂單清單
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o => o.OrderNumber.Contains(search)
                                      || o.CustomerName.Contains(search));

            if (!string.IsNullOrEmpty(status) && status != "全部")
                query = query.Where(o => o.Status == status);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // 用 SelectList 包裝，Tag Helper 可以自動選中
            ViewBag.StatusList = new SelectList(
                new List<string> { "全部", "待處理", "已出貨", "已完成", "已取消" },
                selectedValue: status
            );

            ViewBag.CurrentSearch = search;

            return View(orders);
        }

        // 訂單詳細
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // 建立 SelectList，自動選中目前狀態
            ViewBag.StatusList = new SelectList(
                new List<string> { "待處理", "已出貨", "已完成", "已取消" },
                selectedValue: order.Status
            );

            return View(order);
        }

        // 更新訂單狀態
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;

            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "更新失敗，請稍後再試");
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
