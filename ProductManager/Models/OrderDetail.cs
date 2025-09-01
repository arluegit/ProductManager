using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "數量必須大於0")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "單價不能小於0")]
        public decimal UnitPrice { get; set; }

        // 導覽屬性
        public Product Product { get; set; }
        public Order Order { get; set; }
    }
}
