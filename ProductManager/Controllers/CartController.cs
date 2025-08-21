using Microsoft.AspNetCore.Mvc;
using ProductManager.Models;
using ProductManager.Helpers;


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
                    Quantity = 1
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
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);

            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = quantity;

                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            var totalAmount = cart.Sum(c => c.Price * c.Quantity);
            var itemTotal = item != null ? item.Price * item.Quantity : 0;

            return Json(new { success = true, itemTotal, totalAmount });
        }
        [HttpPost]
        


        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart"); // 移除購物車 Session
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
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return Json(new { success = true });
        }

    }
}
