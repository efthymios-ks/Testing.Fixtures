using Testing.Fixtures.Core;

namespace Testing.Fixtures.Sql;

public sealed class SqlDatabaseFixtureOptions
{
    private int _databaseCount = FixtureDefaults.MaxParallelThreads;

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

    /// <summary>The migrated database every pooled one is restored from.</summary>
    public string TemplateDatabaseName { get; set; } = "FixturesTemplate";

    /// <summary>Where the container keeps its data files; the template backup is written there.</summary>
    public string DataDirectory { get; set; } = "/var/opt/mssql/data";

    /// <summary>The connection string is published under this host setting.</summary>
    public string SettingKey { get; set; } = "ConnectionStrings:Database";

    /// <summary>How long a test waits for a free database before failing rather than hanging.</summary>
    public TimeSpan LeaseTimeout { get; set; } = FixtureDefaults.LeaseTimeout;

    /// <summary>Restore and reset run against a cold container, so they get their own timeout.</summary>
    public int CommandTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Rows a migration seeded are put back after a reset, so a test that changed reference
    /// data cannot leak into the next one. Off when nothing is seeded — it costs a pass over the
    /// template on start-up.
    /// </summary>
    public bool RestoreSeededData { get; set; } = true;

    public string BackupPath
        => $"{DataDirectory.TrimEnd('/')}/{TemplateDatabaseName}.bak";
}
