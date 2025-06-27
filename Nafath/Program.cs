using Habanero.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nafath.Data;
using Nafath.Models;


var builder = WebApplication.CreateBuilder(args);

// 1. Add Connection String and DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add Identity Services
// THIS IS THE CRUCIAL PART. Notice it's AddDefaultIdentity<ApplicationUser>
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>(); // This links Identity to your DbContext

// ... rest of the code


// 3) MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

// **VERY IMPORTANT**: authentication _before_ authorization
app.UseAuthentication();
app.UseAuthorization();

// 6) Endpoint mapping
app.MapRazorPages();  // Identity UI: /Identity/Account/{Login,Register,…}
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
