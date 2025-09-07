using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Models;

namespace SkladisteRobe.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Korisnik> Korisnici { get; set; }
        public DbSet<Materijal> Materijali { get; set; }
        public DbSet<Transakcija> Transakcije { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Korisnik>()
                        .Property(k => k.Role)
                        .HasConversion<string>();

            modelBuilder.Entity<Materijal>()
                        .Property(m => m.Jedinica)
                        .HasConversion<string>();
        }
    }
}