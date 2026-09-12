namespace Testing.Fixtures.Host.Features.Quotes;

/// <summary>What a caller asks for: where a parcel goes, and how heavy it is.</summary>
public sealed class QuoteRequest
{
    public required string Destination { get; init; }

    public required decimal WeightKg { get; init; }
}
