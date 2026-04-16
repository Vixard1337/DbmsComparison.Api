using DbmsComparison.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.ProfileImage).IsRequired(false);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.Duration).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.Metadata).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Latitude).IsRequired();
            entity.Property(x => x.Longitude).IsRequired();
        });

        var providerName = Database.ProviderName;
        if (providerName is not null)
        {
            var metadataProperty = modelBuilder.Entity<Product>().Property(x => x.Metadata);

            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                metadataProperty.HasColumnType("jsonb");
            }
            else if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                metadataProperty.HasColumnType("json");
            }
            else if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                metadataProperty.HasColumnType("TEXT");
            }
        }

        modelBuilder.Entity<ProductCategory>()
            .HasKey(x => new { x.ProductId, x.CategoryId });

        modelBuilder.Entity<Order>()
            .HasOne(x => x.User)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Order)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Product)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId);

        modelBuilder.Entity<ProductCategory>()
            .HasOne(x => x.Product)
            .WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.ProductId);

        modelBuilder.Entity<ProductCategory>()
            .HasOne(x => x.Category)
            .WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.CategoryId);

        modelBuilder.Entity<Category>()
            .HasOne(x => x.ParentCategory)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
