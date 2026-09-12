namespace Testing.Fixtures.Host.Features.Quotes;

public static class DependencyInjection
{
    public static IServiceCollection AddQuotes(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IQuoteService, QuoteService>();

        return services;
    }
}
