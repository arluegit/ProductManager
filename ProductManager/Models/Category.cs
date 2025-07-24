using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入分類名稱")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
