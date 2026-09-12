namespace Testing.Fixtures.Host.Features.Quotes;

public static class QuotesEndpoints
{
    public static IEndpointRouteBuilder MapQuotes(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/quotes", QuoteAsync);
        endpoints.MapGet("/api/v1/quotes/{reference}", FindAsync);

        return endpoints;
    }

    private static async Task<IResult> QuoteAsync(
        QuoteRequest request,
        IQuoteService quotes,
        CancellationToken cancellationToken
    )
    {
        if (request.WeightKg <= 0)
        {
            return Results.BadRequest(new { Error = "The weight has to be greater than zero." });
        }

        var quote = await quotes.QuoteAsync(request, cancellationToken);

        return quote is null
            ? Results.NotFound(new { Error = $"Nothing ships to '{request.Destination}'." })
            : Results.Ok(quote);
    }

    private static async Task<IResult> FindAsync(
        string reference,
        IQuoteService quotes,
        CancellationToken cancellationToken
    )
    {
        var quote = await quotes.FindAsync(reference, cancellationToken);

        return quote is null ? Results.NotFound() : Results.Ok(quote);
    }
}
