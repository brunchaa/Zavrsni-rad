using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Models;

namespace SkladisteRobe.Data
{
    public class AppDbContext : IdentityDbContext<Korisnik> // Novo: IdentityDbContext za role i autentikaciju
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Materijal> Materijali { get; set; }
        public DbSet<Transakcija> Transakcije { get; set; }
        public DbSet<UlazIzlaz> UlazIzlazi { get; set; } // Ako koristiš, zadržano

        // Novo: DbSet za logove aktivnosti (praćenje login dužine)
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Zadržano: Konfiguracija za enum u Korisnik (ali Identity handla role sada)
            modelBuilder.Entity<Korisnik>()
                        .Property(k => k.Role)
                        .HasConversion<string>();

            modelBuilder.Entity<Materijal>()
                        .Property(m => m.Jedinica)
                        .HasConversion<string>();
        }
    }
}