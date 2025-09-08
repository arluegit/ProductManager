using Microsoft.AspNetCore.Mvc;
using ProductManager.Models;
using ProductManager.Helpers;
using System.Net;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Claims;


namespace ProductManager.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem != null)
            {
                cartItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImagePath = product.ImagePath // ← 加這個
                });
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index", "Products");
        }

        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult UpdateQuantity(int id, int quantity) 
        {
            // 取得購物車
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            // 找到要更新的商品
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    // 數量 <=0 就刪除
                    cart.Remove(item);
                }
                else
                {
                    // 更新數量
                    item.Quantity = quantity;
                }

                // 存回 Session
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            // 導回購物車頁面
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantityAjax(int id, int quantity)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return Json(new { success = false, message = "商品不存在" });

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else if (item.Quantity + quantity > product.Stock)
                {
                    return Json(new { success = false, message = $"庫存不足，剩餘 {product.Stock}" });
                }
                else
                {
                    item.Quantity = quantity;
                }

                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            var totalAmount = cart.Sum(c => c.Price * c.Quantity);
            var itemTotal = cart.FirstOrDefault(c => c.ProductId == id)?.Price *
                cart.FirstOrDefault(c => c.ProductId == id)?.Quantity ?? 0;


            return Json(new { success = true, itemTotal, totalAmount });
        }
        
        


        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);  // 移除購物車 Session
            return RedirectToAction("Index");   // 清空後回購物車頁面
        }


        [HttpPost]
        public IActionResult AddToCartAjax(int id, int quantity)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return Json(new { success = false, message = "商品不存在" });

            if (quantity <= 0)
                return Json(new { success = false, message = "數量必須大於 0" });

            if (quantity > product.Quantity)
                return Json(new { success = false, message = $"庫存不足，剩餘 {product.Quantity}" });

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);

            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    return Json(new { success = false, message = $"庫存不足，剩餘 {product.Quantity}" });
                }
                
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImagePath = product.ImagePath
                });
            }        

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return Json(new { success = true });
        }

        // GET: Cart/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            if (!cart.Any()) return RedirectToAction("Index");

            var model = new CheckoutViewModel
            {
                Items = cart,
                CustomerName = User.Identity?.Name ?? "", // 直接使用 ClaimTypes.Name
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? ""
            };

            return View(model);
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            model.Items = cart;

            if (!cart.Any())
            {
                ModelState.AddModelError("", "購物車是空的，無法結帳");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 取得資料庫商品
            var productIds = cart.Select(c => c.ProductId).ToList();
            var products = _context.Products
                                   .Where(p => productIds.Contains(p.Id))
                                   .ToDictionary(p => p.Id);

            foreach (var item in cart)
            {
                if (!products.ContainsKey(item.ProductId))
                {
                    ModelState.AddModelError("", $"找不到商品 {item.Name}");
                    return View(model);
                }

                var product = products[item.ProductId];
                if (product.Quantity < item.Quantity) // ✅ 使用 Quantity
                {
                    ModelState.AddModelError("", $"商品 {item.Name} 庫存不足，剩餘 {product.Quantity} 件");
                    return View(model);
                }
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                string today = DateTime.Now.ToString("yyyyMMdd");
                int count = _context.Orders.Count(o => o.OrderDate.Date == DateTime.Today) + 1;
                //string orderId = $"{today}{count:D4}";
                // 更安全的訂單編號生成
                string orderId = $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString().Substring(0, 4)}";


                // 重新抓取登入使用者，避免用 POST 的空值
                //var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                User? currentUser = null;
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim);
                    currentUser = _context.Users.Find(userId);
                }

                var order = new Order
                {
                    OrderNumber = orderId,
                    CustomerName = currentUser?.Username ?? model.CustomerName, // ✅ 優先使用登入者名稱
                    Phone = model.Phone,
                    Email = currentUser?.Email ?? model.Email,                 // ✅ 優先使用登入者 Email
                    Address = model.Address,
                    OrderDate = DateTime.Now,
                    TotalAmount = model.TotalAmount,
                    OrderDetails = cart.Select(c => new OrderDetail
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Price
                    }).ToList()

                };

                _context.Orders.Add(order);

                // ✅ 扣庫存（正確使用 Quantity）
                foreach (var item in cart)
                {
                    var product = products[item.ProductId];
                    product.Quantity -= item.Quantity;
                    _context.Products.Update(product);
                }

                _context.SaveChanges();
                transaction.Commit();

                

                HttpContext.Session.SetObject("TempOrder", new CheckoutViewModel
                {
                    CustomerName = order.CustomerName,
                    Email = order.Email,
                    Phone = order.Phone,
                    Address = order.Address,
                    Items = cart,
                    OrderId = order.OrderNumber,       // ✅ 加這行
                    CreatedAt = order.OrderDate        // ✅ 加這行
                });
                HttpContext.Session.Remove(CartSessionKey);

                return RedirectToAction("CheckoutSuccess");
            }
            catch
            {
                transaction.Rollback();
                ModelState.AddModelError("", "訂單建立失敗，請稍後再試");
                return View(model);
            }
        }


        // GET: Cart/CheckoutSuccess
        public IActionResult CheckoutSuccess()
        {
            var orderVM = HttpContext.Session.GetObject<CheckoutViewModel>("TempOrder");
            if (orderVM == null) return RedirectToAction("Index");
            return View(orderVM);
        }

    }
}
