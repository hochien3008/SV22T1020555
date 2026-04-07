using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Shop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SV22T1020555.Shop";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var culture = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration);

var conn = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:LiteCommerceDB trong appsettings.json.");
Configuration.Initialize(conn);

app.Run();
