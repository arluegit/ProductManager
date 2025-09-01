// Models/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "請輸入帳號")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "請輸入Email")]
    [EmailAddress(ErrorMessage = "Email格式錯誤")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "請再次輸入密碼")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "密碼與確認密碼不一致")]
    public string ConfirmPassword { get; set; } = "";
}