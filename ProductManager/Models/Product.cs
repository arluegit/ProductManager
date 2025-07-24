using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManager.Models
{

    public class Product
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "請輸入產品名稱")]
        [StringLength(100, ErrorMessage = "產品名稱長度不能超過 100 個字")]
        public string? Name { get; set; }

        [Range(1, 1000000, ErrorMessage = "價格必須大於 0")]
        public int Price { get; set; }

        [Range(0, 1000000, ErrorMessage = "數量不能小於 0")]
        public int Quantity { get; set; }

        // 這裡是 CreatedDate 屬性
        public DateTime CreatedDate { get; set; }    // 表示不可為 null

        public string? ImagePath { get; set; } // 儲存圖片相對路徑或檔名


        [Display(Name = "分類")]
        [Required(ErrorMessage = "請選擇分類")]
        public int? CategoryId { get; set; }  // 外鍵

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; } // 導覽屬性


        //products上架
        public bool IsAvailable { get; set; } = true; // 預設為 true，表示產品上架中
        
    }


}
