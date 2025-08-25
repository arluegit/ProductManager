namespace ProductManager.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }  
        public decimal SubTotal => Quantity * Price;
        public int Stock { get; set; } // 新增庫存屬性
    }
}
