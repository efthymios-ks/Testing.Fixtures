using Testing.Fixtures.Core;
using Testing.Fixtures.Host.Features.Persistence;
using Testing.Fixtures.Redis;
using Testing.Fixtures.Settings;
using Testing.Fixtures.Sql;

namespace Testing.Fixtures.Host.Tests.Core;

/// <summary>
/// What the quotes host needs to run: quiet logging, a SQL Server with a database per test, and a
/// Redis database per test. The order is the start order — the engine before the databases that
/// restore from it.
/// </summary>
public sealed class QuotesFixture : ServiceTestFixture
{
    protected override TestConfiguration[] CreateConfigurations() =>
    [
        new SettingsTestConfiguration(),
        new SqlServerTestConfiguration(),
        new SqlDatabaseTestConfiguration<QuotesDbContext>(
            new SqlDatabaseFixtureOptions
            {
                TemplateDatabaseName = "QuotesTemplate"
            }
        ),
        new RedisTestConfiguration()
    ];
}

[CollectionDefinition(nameof(QuotesServiceTests))]
public sealed class QuotesServiceTests : ICollectionFixture<QuotesFixture>;
