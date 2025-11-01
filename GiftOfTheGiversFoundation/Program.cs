using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ? Register ApplicationDbContext using your Azure SQL connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ? Configure Email Settings from appsettings.json
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// ? Register Email Service for Dependency Injection
builder.Services.AddTransient<IEmailSender, EmailService>();

// ? Add MVC Controllers with Views
builder.Services.AddControllersWithViews();

// ? Add Session Support (CRITICAL for your TwoFactor flow)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ? Add Authentication with Cookie Scheme (CRITICAL)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// ? Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// ? Configure middleware IN CORRECT ORDER
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ? Session MUST come before Routing
app.UseSession();

app.UseRouting();

// ? Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ? Default route (login page)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
// Make Program class accessible for integration testing
public partial class Program { }