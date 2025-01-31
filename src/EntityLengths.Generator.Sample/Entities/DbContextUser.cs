using Microsoft.EntityFrameworkCore;

namespace EntityLengths.Generator.Sample.Entities;

/// <summary>
///  Represents a user entity with data annotation configuration.
/// </summary>
public class DbContextUser
{
    public required string Name { get; set; }
}

public class SampleDbContextUser : DbContext
{
    public DbSet<DbContextUser> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbContextUser>().Property(b => b.Name).HasMaxLength(50).IsRequired();
    }
}
