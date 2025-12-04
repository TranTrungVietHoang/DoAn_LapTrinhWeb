using HShop.Data;
using HShop.Helpers;
using HShop.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1️⃣ Đọc file cấu hình (appsettings.json)
// =============================
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// =============================
// 2️⃣ Cấu hình DbContext
// =============================
builder.Services.AddDbContext<Hshop2023Context>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("HShop");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("❌ Connection string 'HShop' không tồn tại hoặc rỗng trong appsettings.json");
    }

    options.UseSqlServer(connectionString);
});

// =============================
// 3️⃣ Add Controller + View
// =============================
builder.Services.AddControllersWithViews();

// =============================
// 4️⃣ Cấu hình Session
// =============================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =============================
// 5️⃣ Cấu hình AutoMapper
// =============================
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// =============================
// 6️⃣ Cấu hình Cookie Authentication (ĐĂNG NHẬP / PHÂN QUYỀN)
// =============================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap";        // Trang đăng nhập
        options.LogoutPath = "/KhachHang/DangXuat";       // Trang đăng xuất
        options.AccessDeniedPath = "/KhachHang/AccessDenied"; // Trang bị từ chối truy cập
        options.ExpireTimeSpan = TimeSpan.FromHours(2);   // Cookie tồn tại 2h
        options.SlidingExpiration = true;                 // Tự gia hạn khi còn hạn
    });

// ✅ Bật phân quyền
builder.Services.AddAuthorization();

// =============================
// 7️⃣ Đăng ký Paypal client (Singleton)
// =============================
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]
));

// =============================
// 8️⃣ Đăng ký VnPay service
// =============================
builder.Services.AddSingleton<IVnPayService, VnPayService>();

var app = builder.Build();

// =============================
// 9️⃣ Cấu hình pipeline HTTP
// =============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Bắt buộc: Session + Auth + Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =============================
// 🔟 Định tuyến mặc định
// =============================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// =============================
// 11️⃣ Log connection string ra console (debug)
// =============================
Console.WriteLine($"✅ Đang sử dụng ConnectionString: {builder.Configuration.GetConnectionString("HShop")}");


app.Run();
