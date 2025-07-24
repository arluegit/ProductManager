using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManager.Models;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync(); // ← 從資料庫查出總人數
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var totalProducts = await _context.Products.CountAsync();
        var totalCategories = await _context.Category.CountAsync();
        var recentUsers = await _context.Users
            .OrderByDescending(u => u.Id)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalUsers = totalUsers;// ← 存進 ViewBag 傳給 View
        ViewBag.ActiveUsers = activeUsers;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.RecentUsers = recentUsers;

        return View();
    }
}
