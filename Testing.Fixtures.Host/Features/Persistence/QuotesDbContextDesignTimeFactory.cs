using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Testing.Fixtures.Host.Features.Persistence;

/// <summary>What `dotnet ef` builds the context with; the host is not started for a migration.</summary>
public sealed class QuotesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<QuotesDbContext>
{
    public QuotesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuotesDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=Quotes;Integrated Security=True;TrustServerCertificate=True");

        return new QuotesDbContext(optionsBuilder.Options);
    }
}
