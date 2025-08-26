using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class Order
    {
        public int Id { get; set; } // 資料庫主鍵
        public string OrderId { get; set; } //  自訂訂單編號
        [Required]
        public DateTime OrderDate { get; set; }
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }
        public int TotalAmount { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }

    public class OrderViewModel
    {
        public String OrderId { get; set; } // 假的訂單編號
        public DateTime CreatedAt { get; set; } = DateTime.Now; // 假的建立時間
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // 自動計算總金額
        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
    }
}
