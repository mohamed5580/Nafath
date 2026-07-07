using Domin.Entity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<Domin.Entity.ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "471B7DBC-9503-479A-A187-756C81150B8C",
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "D25F24F2-0D34-40CB-8BED-EE8F4CE8568E"
            },
            new IdentityRole
            {
                Id = "B0DB744E-4552-45E2-9458-640CC402F915",
                Name = "SuperAdmin",
                NormalizedName = "SUPERADMIN",
                ConcurrencyStamp = "39EDDF3F-BC2D-4A31-89EE-17F22694C947"
            },
            new IdentityRole
            {
                Id = "C0234CD1-C1C1-40D1-B6BC-A497B6887F44",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "F5FF5D8B-5DB6-4B3E-8E18-6DC14B9135F8"
            });

            builder.Entity<Chairs>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(50);
   
            builder.Entity<VwUsers>(entity =>
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
            // Configure relationship between Order and Identity user
            builder.Entity<Order>(b =>
            {
                // Order.User (Infrastructure.Models.ApplicationUser) -> AspNetUsers.Id
                b.HasOne(o => o.User)
                 .WithMany() // no navigation collection on ApplicationUser
                 .HasForeignKey(o => o.UserId)
                 .HasPrincipalKey(u => u.Id)
                 .OnDelete(DeleteBehavior.Restrict);

                b.Property(o => o.OrderStatus)
                 .HasMaxLength(50)
                 .IsRequired();

                b.Property(o => o.MobileNumber)
                 .HasMaxLength(50)
                 .IsRequired();
            });


            builder.Entity<OrderItem>(oi =>
            {
                // ربط صحيح لـ Product
                oi.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

                // ربط صحيح لـ Order
                oi.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

                oi.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
                oi.Property(x => x.TotalPrice)
                  .HasPrecision(18, 2)
                  .HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: true);

            });


            builder.Entity<ProductType>(b =>
            {
                b.Property(pt => pt.Name).IsRequired().HasMaxLength(100);
            });

            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Price).HasPrecision(18, 2);
                b.HasOne(p => p.ProductType)
                 .WithMany(pt => pt.Products)
                 .HasForeignKey(p => p.ProductTypeId);
            });

        }

        public DbSet<Chairs> Chairs { get; set; }
        public DbSet<VwUsers> VwUsers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
   
    }

}
