using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManager.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }   // 明細編號 (PK)

        [Required]
        public int OrderId { get; set; }   // 關聯到訂單

        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        [Required]
        public int ProductId { get; set; }  // 關聯到商品

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required]
        public int Quantity { get; set; }   // 購買數量

        [Required]
        public decimal UnitPrice { get; set; }   // 當下單價 (避免商品後來改價)
    }
}
