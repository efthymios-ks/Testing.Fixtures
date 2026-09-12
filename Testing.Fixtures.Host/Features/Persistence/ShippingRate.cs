namespace Testing.Fixtures.Host.Features.Persistence;

public sealed class ShippingRate
{
    public int Id { get; set; }

    public string Destination { get; set; } = null!;

    public decimal PricePerKg { get; set; }
}
