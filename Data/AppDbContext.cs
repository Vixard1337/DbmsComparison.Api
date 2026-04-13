using DbmsComparison.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConnectionProbe> ConnectionProbes => Set<ConnectionProbe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConnectionProbe>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
