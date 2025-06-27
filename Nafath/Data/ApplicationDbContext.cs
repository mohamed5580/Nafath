using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nafath.Models;

namespace Nafath.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Chairs>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(50);
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "117af781-a39d-4d33-86bb-3e0e6746ded7",
                    Name = "Admin",
                    NormalizedName = "admin",
                    ConcurrencyStamp = "2330d643-eefc-4f16-8c8b-4cb26723b363"
                },
                new IdentityRole
                {
                    Id = "499cb10d-cdab-45b4-91fc-7a3bf90e0edf",
                    Name = "User",
                    NormalizedName = "user",
                    ConcurrencyStamp = "7376f771-4753-4c13-9ac0-977095a13911"
                }
            );
        }
        public DbSet<Chairs> Chairs { get; set; }

    }
}
