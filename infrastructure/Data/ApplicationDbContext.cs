using Domin.Entity;
using Infrastructure.Models;
using Infrastructure.Models.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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


        }

        public DbSet<Chairs> Chairs { get; set; }
        public DbSet<VwUser> VwUsers { get; set; }

    }
}
