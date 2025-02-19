using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityLengths.Generator.Sample.Entities;

/// <summary>
/// Represents a user entity with fluent configuration.
/// </summary>
public class FluentUser
{
    public required string Name { get; set; }

    public required string Description { get; set; }
}

public class FluentConfigurationUser : IEntityTypeConfiguration<FluentUser>
{
    public void Configure(EntityTypeBuilder<FluentUser> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500).IsRequired();
    }
}
