using Microsoft.AspNetCore.Mvc;
using ProductManager.Models;
using ProductManager.Helpers;
using System.Net;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;


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
                else if (quantity > product.Stock)
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
                    ImagePath = product.ImagePath,
                    Stock = product.Quantity
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
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }
            return View(cart); // 顯示 Checkout.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string customerName, string phone, string email, string address)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            string today = DateTime.Now.ToString("yyyyMMdd");
            int count = _context.Orders.Count(o => o.OrderDate.Date == DateTime.Today) + 1;
            string orderId = $"{today}{count:D4}";

            var order = new Order
            {
                OrderId = orderId,
                CustomerName = customerName,
                Phone = phone,
                Email = email,
                Address = address,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                OrderDetails = cart.Select(c => new OrderDetail
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.SaveChanges(); // 存入資料庫

            var orderVM = new OrderViewModel
            {
                OrderId = order.OrderId,
                CreatedAt = order.OrderDate,
                Items = cart.Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Name,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    ImagePath = c.ImagePath,
                    Stock = c.Stock
                }).ToList()
            };

            HttpContext.Session.SetObject("TempOrder", orderVM);
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("CheckoutSuccess");
        }

        // GET: Cart/CheckoutSuccess
        public IActionResult CheckoutSuccess()
        {
            var orderVM = HttpContext.Session.GetObject<OrderViewModel>("TempOrder");
            if (orderVM == null) return RedirectToAction("Index");
            return View(orderVM);
        }

    }
}
