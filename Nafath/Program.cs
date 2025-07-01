using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.IRepository.Base;
using Infrastructure.Models;
using Domin.Entity;
using Habanero.Util;
using Nafath.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Connection String and DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add Identity Services
// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddSession(); // Add session support
// ✅ Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ... rest of the code
builder.Services.AddTransient(typeof(IRepository<>), typeof(MainRepository<>));


// 3) MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddTransient<IEmailSender, Nafath.Services.EmailSender>();


var app = builder.Build();

// 4) Error pages & HSTS
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 5) Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ Enable session middleware
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Required to serve the built-in Identity Razor Pages:
app.MapRazorPages();

// Required to
app.MapControllerRoute(
  name: "areas",
  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"); 
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Accounts}/{action=Login}/{id?}");
app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();
    