using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using SkladisteRobe.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day));

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Korisnik, IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseMiddleware<UserActivityMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Seeding block with migration to ensure database and tables exist before seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // Apply migrations automatically to create tables like AspNetRoles

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = services.GetRequiredService<UserManager<Korisnik>>();

    string[] roles = { "Admin", "Voditelj", "Radnik" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        adminUser = new Korisnik { UserName = "admin", Email = "admin@example.com", Ime = "Admin", Prezime = "Admin", Role = Uloga.Admin };
        await userManager.CreateAsync(adminUser, "AdminPass123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();