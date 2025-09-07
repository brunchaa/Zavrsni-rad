using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using SkladisteRobe.Middleware; // Novo: Middleware za praæenje
using Serilog; // Novo: Za logging

var builder = WebApplication.CreateBuilder(args);

// Novo: Dodaj Serilog za logging aktivnosti (piše u konzolu i file)
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day));

// Konfiguracija za bazu (zadržano)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Novo: Dodaj Identity za role i autentikaciju (zamjena za custom cookie; handla hash, role)
builder.Services.AddIdentity<Korisnik, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Zadržano: MVC servisi
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Novo: Middleware za praæenje korisnièke aktivnosti (ažurira LastActivityTime na svakom requestu)
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Novo: Seed role i default admin user (pokreni jednom; kreira role i admina ako nema)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<Korisnik>>();

    string[] roles = { "Admin", "Voditelj", "Radnik" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Kreiraj default admin
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser == null)
    {
        adminUser = new Korisnik { UserName = "admin", Email = "admin@example.com", Ime = "Admin", Prezime = "Admin", Role = Uloga.Admin }; // Prilagodio tvojim poljima
        await userManager.CreateAsync(adminUser, "AdminPass123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();