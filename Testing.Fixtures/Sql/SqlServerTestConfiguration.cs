using Testcontainers.MsSql;
using Testing.Fixtures.Core;

namespace Testing.Fixtures.Sql;

/// <summary>
/// Owns the SQL Server engine for the run and publishes its connection, which the database
/// configurations build on.
/// </summary>
public sealed class SqlServerTestConfiguration(SqlServerFixtureOptions options) : TestConfiguration
{
    private readonly SqlServerFixtureOptions _options = options;

    private MsSqlContainer? _container;

    public SqlServerTestConfiguration()
        : this(new SqlServerFixtureOptions())
    {
    }

    public override async Task GlobalSetupAsync(GlobalTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _container = new MsSqlBuilder(_options.Image).Build();

        await _container.StartAsync();

        context.Set(new SqlServerInstance(_container.GetConnectionString()));
    }

    public override async Task GlobalTearDownAsync(GlobalTestContext context)
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
