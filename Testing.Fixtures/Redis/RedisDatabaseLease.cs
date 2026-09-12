using StackExchange.Redis;

namespace Testing.Fixtures.Redis;

/// <summary>
/// One numbered Redis database, held for the length of a test. Tests run in parallel against the
/// same container, so each takes its own database rather than its own container.
/// </summary>
public sealed record RedisDatabaseLease(string ConnectionString, int DatabaseIndex)
{
    public string ScenarioConnectionString
        => $"{ConnectionString},defaultDatabase={DatabaseIndex}";

    public async Task<IConnectionMultiplexer> ConnectAsync()
        => await ConnectionMultiplexer.ConnectAsync(ScenarioConnectionString);
}
