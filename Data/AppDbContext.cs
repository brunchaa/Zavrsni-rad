using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Models;

namespace SkladisteRobe.Data
{
    public class AppDbContext : IdentityDbContext<Korisnik, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Materijal> Materijali { get; set; }
        public DbSet<Transakcija> Transakcije { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Korisnik>().Property(k => k.Role).HasConversion<string>();
            modelBuilder.Entity<Materijal>().Property(m => m.Jedinica).HasConversion<string>();

            // Ignore non-mapped properties in Korisnik to avoid mapping errors
            modelBuilder.Entity<Korisnik>().Ignore(k => k.Roles);
            modelBuilder.Entity<Korisnik>().Ignore(k => k.Password); // Ignore Password as Identity handles it
            modelBuilder.Entity<Korisnik>().Ignore(k => k.Username); // Ignore if duplicating UserName; use Identity's UserName instead
        }
    }
}