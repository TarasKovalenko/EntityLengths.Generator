using Microsoft.EntityFrameworkCore;

namespace EntityLengths.Generator.Sample.Entities;

/// <summary>
///  Represents a user entity with data annotation configuration.
/// </summary>
public class DbContextUser
{
    public required string Name { get; set; }
    public required string Description { get; set; }

    public required string Name2 { get; set; }
    public required string Description2 { get; set; }

    public int Age { get; set; }
}

public class SampleDbContextUser : DbContext
{
    public DbSet<DbContextUser> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // lambda expression
        modelBuilder.Entity<DbContextUser>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
        });

        // method chaining
        modelBuilder.Entity<DbContextUser>().Property(b => b.Name2).HasMaxLength(50).IsRequired();
        modelBuilder
            .Entity<DbContextUser>()
            .Property(b => b.Description2)
            .HasMaxLength(500)
            .IsRequired();
    }
}
