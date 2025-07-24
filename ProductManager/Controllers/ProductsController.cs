        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Hosting;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.AspNetCore.Mvc.Rendering;
        using Microsoft.EntityFrameworkCore;
        using ProductManager.Models;
        using System;
        using System.IO;
        using System.Linq;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Http;

        namespace ProductManager.Controllers
        {
            [Authorize]
            public class ProductsController : Controller
            {
                private readonly AppDbContext _context;
                private readonly IWebHostEnvironment _env;

                public ProductsController(AppDbContext context, IWebHostEnvironment env)
                {
                    _context = context;
                    _env = env;
                }



            [AllowAnonymous]
            public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1)
            {
                int pageSize = 5;

                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(p => p.Name.Contains(keyword));
                }

                if (categoryId != null && categoryId > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId);
                }

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var products = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 加入分類清單供 View 顯示
                var category = await _context.Category.ToListAsync();
                //ViewBag.Category= new SelectList(category, "Id", "Name");
                ViewBag.Category = new SelectList(category, "Id", "Name", categoryId);
                ViewBag.SelectedCategoryId = categoryId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Keyword = keyword;
                //var categoryList = await _context.Category.ToListAsync();
                //ViewBag.Category = new SelectList(categoryList, "Id", "Name", categoryId);
                //ViewBag.SelectedCategoryId = categoryId;
                return View(products);
            }

            [AllowAnonymous]
                public async Task<IActionResult> Details(int? id)
                {
                    if (id == null) return NotFound();

                    var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
                    if (product == null) return NotFound();

                    return View(product);
                }

                [Authorize(Roles = "Admin")]
                public IActionResult Create()
                {
                    var categories = _context.Category.ToList(); // 取得所有分類
                    ViewBag.Category = new SelectList(categories, "Id", "Name"); // 填入 ViewBag
                    return View(); // 回傳 View
                }

                [HttpPost]
                [ValidateAntiForgeryToken]
                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
                {
                //if (User.Identity?.Name != "Admin") return Forbid();

                if (ModelState.IsValid)
                {
                        product.CreatedDate = DateTime.Now;

                        // 圖片上傳
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            if (!imageFile.ContentType.StartsWith("image/") || imageFile.Length > 2 * 1024 * 1024)
                            {
                                ModelState.AddModelError("", "只允許上傳小於 2MB 的圖片（jpg/png）");
                                ViewBag.Category = new SelectList(_context.Category.ToList(), "Id", "Name", product.CategoryId); // 加這行以防 ModelState 失敗時分類還存在
                                return View(product);
                            }

                            var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }
                            product.ImagePath = fileName;
                        }

                        // 儲存產品（包含 CategoryId）
                        _context.Add(product);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }

                    var categories = _context.Category.ToList();
                    ViewBag.Category = new SelectList(categories, "Id", "Name");
                    return View();
                }



                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> Edit(int? id)
                {
                    //if (User.Identity?.Name != "Admin") return Forbid();

                    if (id == null) return NotFound();

                    var product = await _context.Products.FindAsync(id);
                    if (product == null) return NotFound();

                    // 加入分類清單到 ViewBag
                    ViewBag.Category = new SelectList(await _context.Category.ToListAsync(), "Id", "Name", product.CategoryId);

                    return View(product);
                }

                [HttpPost]
                [ValidateAntiForgeryToken]
                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
                {
                    //if (User.Identity?.Name != "Admin") return Forbid();

                    if (id != product.Id) return NotFound();

                    var dbProduct = await _context.Products.FindAsync(id);
                    if (dbProduct == null) return NotFound();

                    if (ModelState.IsValid)
                    {
                        dbProduct.Name = product.Name;
                        dbProduct.Price = product.Price;
                        dbProduct.Quantity = product.Quantity;
                        dbProduct.CategoryId = product.CategoryId; // 記得加這行

                        if (imageFile != null && imageFile.Length > 0)
                        {
                            if (!string.IsNullOrEmpty(dbProduct.ImagePath))
                            {
                                var oldPath = Path.Combine(_env.WebRootPath, "uploads", dbProduct.ImagePath);
                                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                            }

                            var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }
                            dbProduct.ImagePath = fileName;
                        }

                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }

                    // 如果驗證失敗，也要重新設定下拉選單
                    ViewBag.Category = new SelectList(_context.Category.ToList(), "Id", "Name", product.CategoryId);
                    return View(product);
                }

                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> Delete(int? id)
                {
                    //if (User.Identity?.Name != "Admin") return Forbid();

                    if (id == null) return NotFound();

                    var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
                    if (product == null) return NotFound();

                    return View(product);
                }

                [HttpPost, ActionName("Delete")]
                [ValidateAntiForgeryToken]
                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> DeleteConfirmed(int id)
                {
                    //if (User.Identity?.Name != "Admin") return Forbid();

                    var product = await _context.Products.FindAsync(id);
                    if (product != null)
                    {
                        if (!string.IsNullOrEmpty(product.ImagePath))
                        {
                            var imagePath = Path.Combine(_env.WebRootPath, "uploads", product.ImagePath);
                            if (System.IO.File.Exists(imagePath))
                                System.IO.File.Delete(imagePath);
                        }

                        _context.Products.Remove(product);
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }

                [Authorize(Roles = "Admin")]
                public IActionResult Statistics()
                {
                    //if (User.Identity?.Name != "Admin") return Forbid();

                    var years = _context.Products
                        .Select(p => p.CreatedDate.Year)
                        .Distinct()
                        .OrderByDescending(y => y)
                        .ToList();

                    ViewBag.Years = new SelectList(years);

                    return View();
                }

                [HttpGet]
                [Authorize(Roles = "Admin")]
                public IActionResult GetMonthlyProductCount(int year)
                {
                    var monthlyCounts = _context.Products
                        .Where(p => p.CreatedDate.Year == year)
                        .GroupBy(p => p.CreatedDate.Month)
                        .Select(g => new { Month = g.Key, Count = g.Count() })
                        .ToList();

                    int[] data = new int[12];
                    foreach (var item in monthlyCounts)
                    {
                        data[item.Month - 1] = item.Count;
                    }

                    return Json(data);
                }

                [HttpGet]
                public IActionResult AllProducts()
                {
                    var products = _context.Products
                        .Where(p => p.IsActive) // ✅ 只抓上架產品
                        .ToList();
                    return View(products);
                }



        private bool ProductExists(int id)
                {
                    return _context.Products.Any(e => e.Id == id);
                }


                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> CategoryStats()
                {
                    var data = await _context.Products
                        .Include(p => p.Category)
                        .GroupBy(p => p.Category.Name)
                        .Select(g => new
                        {
                            CategoryName = g.Key,
                            ProductCount = g.Count()
                        })
                        .ToListAsync();

                    ViewBag.Categories = data.Select(d => d.CategoryName).ToArray();
                    ViewBag.Counts = data.Select(d => d.ProductCount).ToArray();

                    return View();
                }


                [Authorize(Roles = "Admin")]
                public async Task<IActionResult> Dashboard()
                {
                    var totalProducts = await _context.Products.CountAsync();
                    var totalCategories = await _context.Category.CountAsync();
                    var recentProducts = await _context.Products
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(5)
                        .Include(p => p.Category)
                        .ToListAsync();

                    ViewBag.TotalProducts = totalProducts;
                    ViewBag.TotalCategories = totalCategories;
                    ViewBag.RecentProducts = recentProducts;

                    return View();
                }

                [Authorize(Roles = "Admin")]
                [HttpPost]
                public async Task<IActionResult> ToggleActive(int id)
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null) return NotFound();

                    product.IsActive = !product.IsActive;
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"產品已{(product.IsActive ? "上架" : "下架")}";
                    return RedirectToAction("Index");
                }

            }
        }
