using StackExchange.Redis;

namespace Testing.Fixtures.Host.Features.Caching;

public static class DependencyInjection
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IConnectionMultiplexer>(_
            => ConnectionMultiplexer.Connect(configuration["REDIS_CONNECTION_STRING"]!));

        return services;
    }
}
