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
            // ربط Order.UserId بجدول AspNetUsers دون Navigation Property في الـ Domain
            builder.Entity<Order>(b =>
            {
                // نحدد بوضوح أن Order لديه مستخدم واحد عبر الخاصية "User"
                b.HasOne(o => o.User)
                 // والمستخدم لديه طلبات كثيرة (لا نحدد خاصية هنا لأنها غير موجودة في ApplicationUser)
                 .WithMany()
                 // والمفتاح الأجنبي الذي يربطهما هو "UserId"
                 .HasForeignKey(o => o.UserId)
                 // وهو مطلوب
                 .IsRequired()
                 // وعند حذف المستخدم، لا تفعل شيئًا (لمنع الحذف المتتالي الذي قد يسبب مشاكل)
                 .OnDelete(DeleteBehavior.Restrict);
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
                b.Property(pt => pt.Name)
                 .IsRequired()
                 .HasMaxLength(100);
            });

            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Name);
                b.Property(p => p.Price)
                .HasPrecision(18, 2);
                // لا تكتب b.Property(p => p.ProductTypes)
                b.HasOne(p => p.ProductType)
                 .WithMany(pt => pt.Products)
                 .HasForeignKey(p => p.ProductTypeId);
            });

        }

        public DbSet<Chairs> Chairs { get; set; }
        public DbSet<VwUser> VwUsers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
   
    }

}
