namespace Testing.Fixtures.Host.Features.Quotes;

public sealed class QuoteResponse
{
    public required string Reference { get; init; }

    public required string Destination { get; init; }

    public required decimal PriceEuros { get; init; }

    public required bool FromCache { get; init; }
}
