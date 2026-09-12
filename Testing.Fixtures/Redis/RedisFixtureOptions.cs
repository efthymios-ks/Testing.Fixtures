using Testing.Fixtures.Core;

namespace Testing.Fixtures.Redis;

public sealed class RedisFixtureOptions
{
    private int _databaseCount = FixtureDefaults.MaxParallelThreads;

    /// <summary>Pinned rather than latest, so a run is repeatable.</summary>
    public string Image { get; set; } = "redis:7.4-alpine";

    /// <summary>
    /// How many tests can hold a database at once. Keep it at or above the runner's parallel thread
    /// count, or tests queue behind each other.
    /// </summary>
    public int DatabaseCount
    {
        get => _databaseCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _databaseCount = value;
        }
    }

    /// <summary>The connection string is published under this host setting.</summary>
    public string SettingKey { get; set; } = "REDIS_CONNECTION_STRING";

    /// <summary>How long a test waits for a free database before failing rather than hanging.</summary>
    public TimeSpan LeaseTimeout { get; set; } = FixtureDefaults.LeaseTimeout;
}
