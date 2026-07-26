namespace MyApp.Domain.Entities;

public record Order
{
    public Guid Id { get; init; }

    public string Reference { get; init; } = string.Empty;

    public string ShippingAddress { get; init; } = string.Empty;
}
