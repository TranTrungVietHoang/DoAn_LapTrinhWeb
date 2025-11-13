using HShop.Data;
using HShop.Helpers;
using HShop.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ? ??c file c?u hình (appsettings.json)
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// ? C?u hình DbContext
builder.Services.AddDbContext<Hshop2023Context>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("HShop");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("?? Connection string 'HShop' không t?n t?i ho?c r?ng trong appsettings.json");
    }

    options.UseSqlServer(connectionString);
});

// ? Add Controller + View
builder.Services.AddControllersWithViews();

// ? C?u hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ? C?u hình AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// ? C?u hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap";
        options.AccessDeniedPath = "/AccessDenied";
    });

// ? ??ng ký Paypal client (Singleton)
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]
));

// ? ??ng ký VnPay service
builder.Services.AddSingleton<IVnPayService, VnPayService>();

var app = builder.Build();

// ? C?u hình pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ? ??nh tuy?n
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ? Log connection string ra console (debug)
Console.WriteLine($"? ?ang s? d?ng ConnectionString: {builder.Configuration.GetConnectionString("HShop")}");

app.Run();
