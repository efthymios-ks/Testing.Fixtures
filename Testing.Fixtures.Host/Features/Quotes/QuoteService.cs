using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testing.Fixtures.Host.Features.Persistence;

namespace Testing.Fixtures.Host.Features.Quotes;

/// <summary>
/// Prices a parcel from the rate table, writes the quote down, and remembers the price in Redis so
/// the same parcel is not priced twice.
/// </summary>
public sealed class QuoteService(QuotesDbContext database, IConnectionMultiplexer cache, TimeProvider timeProvider)
    : IQuoteService
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public async Task<QuoteResponse?> QuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var destination = request.Destination.Trim().ToLowerInvariant();
        var cacheKey = $"quote:{destination}:{request.WeightKg}";
        var cached = await cache.GetDatabase().StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            var previous = JsonSerializer.Deserialize<QuoteResponse>((string)cached!)!;

            return new QuoteResponse
            {
                Reference = previous.Reference,
                Destination = previous.Destination,
                PriceEuros = previous.PriceEuros,
                FromCache = true
            };
        }

        var rate = await database.ShippingRates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Destination == destination, cancellationToken);

        if (rate is null)
        {
            return null;
        }

        var quote = new Quote
        {
            Reference = Guid.NewGuid().ToString("N")[..12],
            Destination = destination,
            WeightKg = request.WeightKg,
            PriceEuros = Math.Round(rate.PricePerKg * request.WeightKg, 2),
            CreatedAt = timeProvider.GetUtcNow()
        };

        database.Quotes.Add(quote);
        await database.SaveChangesAsync(cancellationToken);

        var response = new QuoteResponse
        {
            Reference = quote.Reference,
            Destination = quote.Destination,
            PriceEuros = quote.PriceEuros,
            FromCache = false
        };

        await cache.GetDatabase().StringSetAsync(cacheKey, JsonSerializer.Serialize(response), _cacheDuration);

        return response;
    }

    public async Task<QuoteResponse?> FindAsync(string reference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        var quote = await database.Quotes
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Reference == reference, cancellationToken);

        return quote is null
            ? null
            : new QuoteResponse
            {
                Reference = quote.Reference,
                Destination = quote.Destination,
                PriceEuros = quote.PriceEuros,
                FromCache = false
            };
    }
}
