using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class Order
    {
        public int Id { get; set; } // 資料庫主鍵

        [Required]
        [StringLength(20, ErrorMessage = "訂單編號不可超過20字")]
        //public string OrderId { get; set; } // 自訂訂單編號        
        public string OrderNumber { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "請輸入姓名")]
        [StringLength(100, ErrorMessage = "姓名不可超過100字")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "請輸入電話")]
        [Phone(ErrorMessage = "電話格式錯誤")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "請輸入Email")]
        [EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入地址")]
        [StringLength(200, ErrorMessage = "地址不可超過200字")]
        public string Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "金額不能小於0")]
        public decimal TotalAmount { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }

    }

   
}
