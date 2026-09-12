namespace Testing.Fixtures.Host.Features.Persistence;

public sealed class Quote
{
    public int Id { get; set; }

    public string Reference { get; set; } = null!;

    public string Destination { get; set; } = null!;

    public decimal WeightKg { get; set; }

    public decimal PriceEuros { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
