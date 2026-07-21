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
{
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly("Infrastructure"));

    options.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
// 2. Add Identity Services
// Identity
builder.Services.AddIdentity<Domin.Entity.ApplicationUser, IdentityRole>()
     .AddDefaultUI()                // <— brings in the Razor pages for login/register/confirm
    .AddDefaultTokenProviders()    // <— for email confirmation, password reset, etc.
    .AddEntityFrameworkStores<ApplicationDbContext>();
// ✅ Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId =
            builder.Configuration["Authentication:GoogleKeys:ClientId"];

        options.ClientSecret =
            builder.Configuration["Authentication:GoogleKeys:ClientSecret"];
    })
    .AddFacebook(options =>
    {
        options.AppId =
            builder.Configuration["Authentication:FacebookKeys:AppId"];

        options.AppSecret =
            builder.Configuration["Authentication:FacebookKeys:AppSecret"];
    });

builder.Services.AddTransient(typeof(IRepository<>), typeof(MainRepository<>));
builder.Services.AddScoped<IDashboardService, DashboardService>();



// 3) MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Accounts/Accounts/Login";
    options.AccessDeniedPath = "/Accounts/Accounts/AccessDenied";
});

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

// Enable session middleware
app.UseSession();

app.UseAuthentication();

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
app.UseStatusCodePages(async context =>
{
    if (context.HttpContext.Response.StatusCode == 403)
    {
        context.HttpContext.Response.Redirect("/Accounts/Accounts/Login");
    }
});
app.UseAuthorization();
app.MapAreaControllerRoute(
    name: "Accounts",
    areaName: "Accounts",
    pattern: "Accounts/{controller=Accounts}/{action=Login}/{id?}");


// Admin area route
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Accounts}/{action=Login}/{id?}");

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();



