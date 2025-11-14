using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Models;

namespace SkladisteRobe.Data
{
    public class AppDbContext : DbContext  // Promijenjeno u DbContext (maknuli Identity)
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Materijal> Materijali { get; set; }
        public DbSet<Transakcija> Transakcije { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<Korisnik> Korisnici { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Korisnik>().Property(k => k.Role).HasConversion<string>();
            modelBuilder.Entity<Materijal>().Property(m => m.Jedinica).HasConversion<string>();

            // Ignore non-mapped properties in Korisnik (samo Roles)
            modelBuilder.Entity<Korisnik>().Ignore(k => k.Roles);
        }
    }
}