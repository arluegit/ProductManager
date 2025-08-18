using Microsoft.EntityFrameworkCore;
using ProductManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;  // 身分驗證

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddControllersWithViews();

// 加入身份驗證服務
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // 未登入自動跳轉登入頁
    });


// 設定 Session 要在builder.Build();之前
builder.Services.AddSession();


var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
// **身份驗證中介軟體要放在 UseAuthorization 前**
app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // 啟用 Session 支援

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "User" }
        );
        context.SaveChanges();
    }
}



app.Run();

//cd D:\source\repos\ProductManager\ProductManager
//dotnet ef database update

