using Microsoft.EntityFrameworkCore;

namespace Testing.Fixtures.Host.Features.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<QuotesDbContext>(options
            => options.UseSqlServer(configuration.GetConnectionString("Database")));

        return services;
    }
}
