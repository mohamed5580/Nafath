using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.IRepository.Base;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Nafath.Services;
using System;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Connection String and DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly("Nafath")    // <- now migrations go here
    )
);

// 2. Add Identity Services
// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
     .AddDefaultUI()                // <— brings in the Razor pages for login/register/confirm
    .AddDefaultTokenProviders()    // <— for email confirmation, password reset, etc.
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
builder.Services.AddTransient<IEmailSender, EmailSender>();


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


// grab the env
var env = app.Services.GetRequiredService<IWebHostEnvironment>();

var uploadsPath = Path.Combine(env.ContentRootPath, "Uploads", "Users");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/user-images"
});

app.MapAreaControllerRoute(
    name: "accounts",
    areaName: "Accounts",
    pattern: "Accounts/{controller=Accounts}/{action=Login}/{id?}");


// Admin area route
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();



