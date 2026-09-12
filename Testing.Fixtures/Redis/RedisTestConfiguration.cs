using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using Testcontainers.Redis;
using Testing.Fixtures.Core;

namespace Testing.Fixtures.Redis;

/// <summary>
/// One Redis container for the run, one numbered database per test. A database is flushed when it
/// is handed back, so the next test to take it starts empty.
/// </summary>
public sealed class RedisTestConfiguration(RedisFixtureOptions options) : TestConfiguration
{
    private readonly RedisFixtureOptions _options = options;
    private readonly ConcurrentQueue<int> _availableDatabases = new(Enumerable.Range(0, options.DatabaseCount));
    private readonly SemaphoreSlim _leases = new(options.DatabaseCount, options.DatabaseCount);

    private RedisContainer? _container;
    private IConnectionMultiplexer? _multiplexer;

    public RedisTestConfiguration()
        : this(new RedisFixtureOptions())
    {
    }

    public override async Task GlobalSetupAsync(GlobalTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _container = new RedisBuilder(_options.Image).Build();

        await _container.StartAsync();

        _multiplexer = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());

        context.Set(new RedisInstance(_container.GetConnectionString()));
    }

    public override async Task TestSetupAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Set(await LeaseDatabaseAsync());
    }

    public override void Configure(ScenarioTestContext context, IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        settings[_options.SettingKey] = context.Get<RedisDatabaseLease>().ScenarioConnectionString;
    }

    public override async Task TestTearDownAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGet<RedisDatabaseLease>(out var lease))
        {
            return;
        }

        await ReleaseDatabaseAsync(lease, context.Logger);
    }

    public override async Task GlobalTearDownAsync(GlobalTestContext context)
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        _leases.Dispose();
    }

    private async Task<RedisDatabaseLease> LeaseDatabaseAsync()
    {
        if (_container is null)
        {
            throw new InvalidOperationException("The Redis container was never started; global setup did not run.");
        }

        // A leaked lease would otherwise hang the run with nothing to point at.
        if (!await _leases.WaitAsync(_options.LeaseTimeout))
        {
            throw new TimeoutException(
                $"No Redis database came free within {_options.LeaseTimeout}. Raise {nameof(RedisFixtureOptions.DatabaseCount)} above the runner's parallel thread count."
            );
        }

        if (!_availableDatabases.TryDequeue(out var databaseIndex))
        {
            _leases.Release();

            throw new InvalidOperationException(
                "A Redis lease was granted with no database behind it; the pool and the semaphore are out of step."
            );
        }

        return new RedisDatabaseLease(_container.GetConnectionString(), databaseIndex);
    }

    private async Task ReleaseDatabaseAsync(RedisDatabaseLease lease, ILogger logger)
    {
        try
        {
            if (_multiplexer is not null)
            {
                await _multiplexer.GetDatabase(lease.DatabaseIndex).ExecuteAsync("FLUSHDB");
            }
        }
        catch (RedisException exception)
        {
            logger.LogWarning(
                "Redis database {DatabaseIndex} could not be flushed: {Message}",
                lease.DatabaseIndex,
                exception.Message
            );
        }
        finally
        {
            _availableDatabases.Enqueue(lease.DatabaseIndex);
            _leases.Release();
        }
    }
}
