using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProductManager.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProductManager.Helpers;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context) => _context = context;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "請輸入帳號與密碼");
            return View();
        }

        // 從資料庫尋找使用者並驗證密碼（實務中建議密碼要 Hash）改成雜湊型
        var hashed = PasswordHelper.HashPassword(password);

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == hashed);

        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "帳號不存在或已被停用");
            return View();
        }


        // 建立 Claims
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),            // 額外放一個自訂的 UserId
            new(ClaimTypes.Name, user.Username),          // 使用者名稱
            new(ClaimTypes.Email, user.Email ?? "")       // Email
        };

        // 加入角色 Claim
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
        }

        //var identity = new ClaimsIdentity(claims, "login");
        // 使用 CookieAuthenticationDefaults.AuthenticationScheme 作為身份驗證方案
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        // 設定持久化登入
        /*await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true // 可讓登入持久化，例如關閉瀏覽器後仍保留登入狀態
            });*/


        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "帳號與密碼必填");
            return View();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "密碼與確認密碼不一致");
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            ModelState.AddModelError("", "此帳號已存在");
            return View();
        }

        if (_context.Users.Any(u => u.Email == email))
        {
            ModelState.AddModelError("", "Email 已被使用");
            return View();
        }

        var newUser = new User
        {
            Username = username,
            Password = PasswordHelper.HashPassword(password), // 雜湊密碼
            Email = email,
            IsActive = true // 明確指定新使用者為啟用狀態
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(); // 先存使用者才能取得 Id

        // 加入角色：查找 Role 表中名稱為 "User" 的角色
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole != null)
        {
            var userRole = new UserRole
            {
                UserId = newUser.Id,
                RoleId = defaultRole.Id
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        TempData["Message"] = "註冊成功，請登入";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }


    /*private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction("Index", "Home");
    }*/

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult UserList(string? search)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search));
        }

        var users = query.ToList();
        ViewBag.Search = search;
        return View(users);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var currentUserName = User.Identity?.Name;

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        // 如果不是 Admin 且不是本人，就禁止
        if (!User.IsInRole("Admin") && user.Username != currentUserName)
            return Forbid();

        ViewBag.AllRoles = await _context.Roles.ToListAsync();
        return View(user);
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, string username, int[] selectedRoleIds)
    {
        var currentUserName = User.Identity?.Name;

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        // 如果不是 Admin 且不是本人，禁止操作
        if (!User.IsInRole("Admin") && user.Username != currentUserName)
            return Forbid();

        // 如果是 Admin，可以修改帳號與角色
        if (User.IsInRole("Admin"))
        {
            user.Username = username;
            user.UserRoles.Clear();
            foreach (var roleId in selectedRoleIds)
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
        }

        // 密碼修改欄位（從 Request.Form 取得）
        var form = HttpContext.Request.Form;
        string newPassword = form["newPassword"];
        string confirmPassword = form["confirmPassword"];

        if (!string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmPassword))
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "新密碼與確認密碼不一致");
                ViewBag.AllRoles = await _context.Roles.ToListAsync();
                return View(user);
            }

            user.Password = PasswordHelper.HashPassword(newPassword);//複雜密碼

        }

        await _context.SaveChangesAsync();
        TempData["Message"] = "資料更新成功";
        TempData["Message"] = "密碼修改成功";
        return RedirectToAction("UserList");
    }

    /*使用SweetAlert 刪除確認框基本
     //因修改View 中改用 AJAX 刪除，且無法共用所以這一段先註解

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            TempData["Error"] = "找不到使用者";
            return RedirectToAction("UserList");
        }

        // 不能刪除自己
        if (user.Username == User.Identity?.Name)
        {
            TempData["Error"] = "無法刪除自己";
            return RedirectToAction("UserList");
        }

        _context.UserRoles.RemoveRange(user.UserRoles);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Message"] = "刪除成功";
        return RedirectToAction("UserList");
    }
    */
    /*View 中改用 AJAX 刪除*/
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return Json(new { success = false, message = "找不到使用者" });
        }

        if (user.Username == User.Identity?.Name)
        {
            return Json(new { success = false, message = "無法刪除自己" });
        }

        _context.UserRoles.RemoveRange(user.UserRoles);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name;

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound();

        return View(user);
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound();

        if (user.Password != PasswordHelper.HashPassword(currentPassword))
        {
            ModelState.AddModelError("", "當前密碼錯誤");
            return View();
        }


        if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "新密碼與確認密碼不一致或為空");
            return View();
        }

        //user.Password = newPassword; 

        user.Password = PasswordHelper.HashPassword(newPassword);// 實務上建議加密儲存
        await _context.SaveChangesAsync();

        TempData["Message"] = "密碼修改成功";
        return RedirectToAction("Profile");
    }

    //啟用
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> ToggleUserActive(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // 不可停用自己
        if (user.Username == User.Identity?.Name)
        {
            TempData["Error"] = "無法停用自己";
            return RedirectToAction("UserList");
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        TempData["Message"] = $"使用者已{(user.IsActive ? "啟用" : "停用")}";
        return RedirectToAction("UserList");
    }



}
