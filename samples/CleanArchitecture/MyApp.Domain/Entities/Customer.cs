namespace MyApp.Domain.Entities;

/// <summary>
/// Plain domain entity: no data annotations, no EF Core reference. The lengths live in the EF
/// configuration in MyApp.Infrastructure.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
