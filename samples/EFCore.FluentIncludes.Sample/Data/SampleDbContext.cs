using EFCore.FluentIncludes.Sample.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Sample.Data;

public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            entity.HasMany(o => o.LineItems)
                .WithOne(li => li.Order)
                .HasForeignKey(li => li.OrderId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasOne(c => c.Address)
                .WithMany()
                .HasForeignKey(c => c.AddressId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LineItem>(entity =>
        {
            entity.HasOne(li => li.Product)
                .WithMany()
                .HasForeignKey(li => li.ProductId);

            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId);
        });

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);
    }
}
