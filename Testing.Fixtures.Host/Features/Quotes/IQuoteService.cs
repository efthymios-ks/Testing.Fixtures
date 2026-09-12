namespace Testing.Fixtures.Host.Features.Quotes;

public interface IQuoteService
{
    /// <summary>Null when nothing is shipped to that destination.</summary>
    Task<QuoteResponse?> QuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default);

    Task<QuoteResponse?> FindAsync(string reference, CancellationToken cancellationToken = default);
}
