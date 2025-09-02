using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "請輸入姓名")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "請輸入電話")]
        [Phone(ErrorMessage = "電話格式錯誤")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "請輸入 Email")]
        [EmailAddress(ErrorMessage = "Email 格式錯誤")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入地址")]
        public string Address { get; set; }

        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // 自動計算總金額
        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
    }
}
