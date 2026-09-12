using Microsoft.Extensions.DependencyInjection;

namespace Testing.Fixtures.Core;

/// <summary>
/// One piece of the test environment — a container, a database, a batch of settings. The fixture
/// runs the setups in order and the teardowns in reverse, so a piece can rely on what came before it.
/// </summary>
public abstract class TestConfiguration
{
    /// <summary>Once per run, before the first test. Start containers here.</summary>
    public virtual Task GlobalSetupAsync(GlobalTestContext context)
        => Task.CompletedTask;

    /// <summary>Once per test, before the host is built. Take resources out of a pool here.</summary>
    public virtual Task TestSetupAsync(ScenarioTestContext context)
        => Task.CompletedTask;

    /// <summary>
    /// Host settings, applied before the application builds its services — which is when a
    /// connection string is read.
    /// </summary>
    public virtual void Configure(ScenarioTestContext context, IDictionary<string, string?> settings)
    {
    }

    /// <summary>Service overrides, applied while the host is being built.</summary>
    public virtual void Configure(ScenarioTestContext context, IServiceCollection services)
    {
    }

    /// <summary>Once per test, after the hosts are down. Return resources to the pool here.</summary>
    public virtual Task TestTearDownAsync(ScenarioTestContext context)
        => Task.CompletedTask;

    /// <summary>Once per run. Stop containers here.</summary>
    public virtual Task GlobalTearDownAsync(GlobalTestContext context)
        => Task.CompletedTask;
}
