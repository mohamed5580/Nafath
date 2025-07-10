using Domin.Entity;
using Infrastructure.Models;
using Infrastructure.Models.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using static NuGet.Packaging.PackagingConstants;

namespace Infrastructure.Data
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
            builder.Entity<NewRole>()
            .HasKey(nr => nr.RoleId);
            builder.Entity<VwUser>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("VwUsers");
            });
            builder.Entity<Chairs>(entity =>
            {
                entity.Property(c => c.Description)
                      .HasMaxLength(500)
                      .IsRequired();
            });
            // ضمن نفس override OnModelCreating:
            builder.Entity<Chairs>()
                .Property(c => c.Price)
                .HasColumnType("decimal(18,2)");

            // سعر الوحدة في الـ OrderItem
            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

        }

        public DbSet<Chairs> Chairs { get; set; }
        public DbSet<VwUser> VwUsers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

    }
}
