using Darbak.Data;
using Darbak.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// Database
// ==============================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==============================
// Identity
// ==============================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ==============================
// Session
// ==============================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==============================
// MVC
// ==============================

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==============================
// Error Handling
// ==============================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ==============================
// HTTPS
// ==============================

app.UseHttpsRedirection();

// ==============================
// Runtime Static Files
// ==============================

var webRootPath =
    app.Environment.WebRootPath
    ?? Path.Combine(
        app.Environment.ContentRootPath,
        "wwwroot");

var productImagesPath =
    Path.Combine(
        webRootPath,
        "images",
        "products");

// Make sure the upload directory exists.
Directory.CreateDirectory(productImagesPath);

// Required for files created dynamically at runtime.
// This serves files directly from wwwroot.
app.UseStaticFiles();

// Explicitly expose dynamically uploaded product images.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider =
        new PhysicalFileProvider(productImagesPath),

    RequestPath =
        "/images/products"
});

// ==============================
// Routing
// ==============================

app.UseRouting();

// ==============================
// Session
// ==============================

app.UseSession();

// ==============================
// Authentication / Authorization
// ==============================

app.UseAuthentication();
app.UseAuthorization();

// ==============================
// Build-time Static Assets
// ==============================

// Keep MapStaticAssets for CSS, JS and other
// build/publish-time optimized assets.
app.MapStaticAssets();

// ==============================
// MVC Routes
// ==============================

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

// ==============================
// Identity Seeder
// ==============================

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.Run();