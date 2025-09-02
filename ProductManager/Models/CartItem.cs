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

        // 只用於前端最大值限制
        public int Stock { get; set; }
    }
}
