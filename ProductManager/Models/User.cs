using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入帳號")]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool IsActive { get; set; } = true; // 預設啟用
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
