using Microsoft.AspNetCore.Authentication.Cookies; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Infrastructure;
using Serilog;
using SkladisteRobe.Data;
using SkladisteRobe.Middleware;
using SkladisteRobe.Models;
using SkladisteRobe.Services; 
QuestPDF.Settings.License = LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);


Environment.SetEnvironmentVariable("ITEXT_BOUNCY_CASTLE_FACTORY_NAME", "bouncy-castle-fips");

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day));

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher<Korisnik>, PasswordHasher<Korisnik>>();

builder.Services.AddScoped<PdfService>();

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


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    var passwordHasher = services.GetRequiredService<IPasswordHasher<Korisnik>>();

    var adminUser = await context.Korisnici.FirstOrDefaultAsync(k => k.Username == "admin");

    if (adminUser == null)
    {
        adminUser = new Korisnik
        {
            Username = "admin",
            Ime = "Admin",
            Prezime = "Admin",
            Role = Uloga.Admin
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "admin123");

        context.Korisnici.Add(adminUser);
        await context.SaveChangesAsync();
    }
    else if (string.IsNullOrEmpty(adminUser.PasswordHash))
    {
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "admin123");
        adminUser.Role = Uloga.Admin;

        await context.SaveChangesAsync();
    }
}
    app.Run();