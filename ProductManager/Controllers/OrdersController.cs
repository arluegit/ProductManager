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
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                                .Where(o => o.Id == id)
                                .FirstOrDefault();

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
