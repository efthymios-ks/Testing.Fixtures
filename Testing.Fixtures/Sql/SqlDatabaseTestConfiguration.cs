using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text;
using Testing.Fixtures.Core;

namespace Testing.Fixtures.Sql;

/// <summary>
/// A pool of databases for one <typeparamref name="TDbContext"/>. Migrations run once, into a
/// template that is backed up; each pooled database is a restore of that backup, so a test never
/// waits for a migration. A database is emptied when it is handed back, and put back in the
/// pool rather than dropped.
/// </summary>
public sealed class SqlDatabaseTestConfiguration<TDbContext>(SqlDatabaseFixtureOptions options) : TestConfiguration
    where TDbContext : DbContext
{
    private const string ResetSql =
        """
        SET NOCOUNT ON;

        DECLARE @sql NVARCHAR(MAX) = N'';

        -- Foreign keys off, so rows can be deleted in any order
        SELECT @sql += N'ALTER TABLE '
                + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                + N' NOCHECK CONSTRAINT ALL;'
        FROM sys.tables t;

        -- Every row goes, except the migrations history
        SELECT @sql += N'DELETE FROM '
                + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';'
        FROM sys.tables t
        WHERE t.name <> N'__EFMigrationsHistory';

        -- What the migrations seeded goes back in
        SET @sql += @referenceRestore;

        -- Foreign keys on again, and checked
        SELECT @sql += N'ALTER TABLE '
                + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                + N' WITH CHECK CHECK CONSTRAINT ALL;'
        FROM sys.tables t;

        EXEC sys.sp_executesql @sql;
        """;

    private readonly SqlDatabaseFixtureOptions _options = options;
    private readonly ConcurrentBag<PooledDatabase> _pool = [];
    private readonly SemaphoreSlim _leases = new(options.DatabaseCount, options.DatabaseCount);

    private string _masterConnectionString = string.Empty;
    private string _seededDataRestoreSql = string.Empty;
    private int _databaseCounter;

    public SqlDatabaseTestConfiguration()
        : this(new SqlDatabaseFixtureOptions())
    {
    }

    public override async Task GlobalSetupAsync(GlobalTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _masterConnectionString = context.Get<SqlServerInstance>().MasterConnectionString;

        await BuildTemplateDatabaseAsync();

        if (_options.RestoreSeededData)
        {
            _seededDataRestoreSql = await BuildSeededDataRestoreScriptAsync();
        }
    }

    public override async Task TestSetupAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!await _leases.WaitAsync(_options.LeaseTimeout))
        {
            throw new TimeoutException(
                $"No database came free within {_options.LeaseTimeout}. Raise {nameof(SqlDatabaseFixtureOptions.DatabaseCount)} above the runner's parallel thread count."
            );
        }

        try
        {
            var database = _pool.TryTake(out var pooled) ? pooled : await CreatePooledDatabaseAsync();

            context.Set(database);
            context.Set(CreateDbContext(database.ConnectionString));
        }
        catch
        {
                _leases.Release();

            throw;
        }
    }

    public override void Configure(ScenarioTestContext context, IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        settings[_options.SettingKey] = context.Get<PooledDatabase>().ConnectionString;
    }

    public override async Task TestTearDownAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGet<PooledDatabase>(out var database))
        {
            return;
        }

        try
        {
            if (context.TryGet<TDbContext>(out var dbContext))
            {
                await dbContext.DisposeAsync();
            }

            await ResetDatabaseAsync(database);
            _pool.Add(database);
        }
        finally
        {
            _leases.Release();
        }
    }

    public override Task GlobalTearDownAsync(GlobalTestContext context)
    {
        _leases.Dispose();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Built by convention: a context with the usual <c>DbContextOptions&lt;TDbContext&gt;</c>
    /// constructor. Override the whole configuration when a context needs more than that.
    /// </summary>
    private static TDbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;
    }

    private async Task BuildTemplateDatabaseAsync()
    {
        await ExecuteAsync(_masterConnectionString, $"CREATE DATABASE [{_options.TemplateDatabaseName}]");

        await using (var dbContext = CreateDbContext(ConnectionStringFor(_options.TemplateDatabaseName)))
        {
            await dbContext.Database.MigrateAsync();
        }

        await ExecuteAsync(
            _masterConnectionString,
            $"BACKUP DATABASE [{_options.TemplateDatabaseName}] TO DISK = N'{_options.BackupPath}' WITH FORMAT, INIT, COPY_ONLY"
        );
    }

    private async Task<PooledDatabase> CreatePooledDatabaseAsync()
    {
        var databaseName = $"FixturesDb_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid():N}";
        var dataDirectory = _options.DataDirectory.TrimEnd('/');

        var restoreSql =
            $"""
            RESTORE DATABASE [{databaseName}] FROM DISK = N'{_options.BackupPath}'
            WITH MOVE N'{_options.TemplateDatabaseName}' TO N'{dataDirectory}/{databaseName}.mdf',
                 MOVE N'{_options.TemplateDatabaseName}_log' TO N'{dataDirectory}/{databaseName}_log.ldf',
                 RECOVERY, NOUNLOAD, REPLACE
            """;

        await ExecuteAsync(_masterConnectionString, restoreSql);

        var database = new PooledDatabase(ConnectionStringFor(databaseName), databaseName);

        // A fresh restore carries the template's rows; a reused one has been reset. Same baseline either way.
        await ResetDatabaseAsync(database);

        return database;
    }

    private async Task ResetDatabaseAsync(PooledDatabase database)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = ResetSql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@referenceRestore", _seededDataRestoreSql);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// The INSERTs that copy the template's seeded rows back. Table and column names come from the
    /// catalog views of the template, so nothing user-supplied reaches the statement.
    /// </summary>
    private async Task<string> BuildSeededDataRestoreScriptAsync()
    {
        await using var connection = new SqlConnection(ConnectionStringFor(_options.TemplateDatabaseName));
        await connection.OpenAsync();

        var seededTables = new List<(string Schema, string Table)>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT s.name, t.name
                FROM sys.tables t
                JOIN sys.schemas s ON s.schema_id = t.schema_id
                JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
                WHERE t.name <> N'__EFMigrationsHistory'
                GROUP BY s.name, t.name
                HAVING SUM(p.rows) > 0
                """;

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                seededTables.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        var script = new StringBuilder();

        foreach (var (schema, table) in seededTables)
        {
            var columns = new List<string>();
            var hasIdentity = false;

            await using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    """
                    SELECT c.name, c.is_identity
                    FROM sys.columns c
                    WHERE c.object_id = OBJECT_ID(@table) AND c.is_computed = 0
                    ORDER BY c.column_id
                    """;
                command.Parameters.AddWithValue("@table", $"[{schema}].[{table}]");

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    columns.Add($"[{reader.GetString(0)}]");
                    hasIdentity |= reader.GetBoolean(1);
                }
            }

            var columnList = string.Join(", ", columns);
            var target = $"[{schema}].[{table}]";
            var source = $"[{_options.TemplateDatabaseName}].[{schema}].[{table}]";

            if (hasIdentity)
            {
                script.AppendLine($"SET IDENTITY_INSERT {target} ON;");
            }

            script.AppendLine($"INSERT INTO {target} ({columnList}) SELECT {columnList} FROM {source};");

            if (hasIdentity)
            {
                script.AppendLine($"SET IDENTITY_INSERT {target} OFF;");
            }
        }

        return script.ToString();
    }

    private async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await command.ExecuteNonQueryAsync();
    }

    private string ConnectionStringFor(string databaseName)
        => new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = databaseName
        }.ConnectionString;
}
