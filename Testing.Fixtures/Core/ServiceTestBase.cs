using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Fixtures.Core;

/// <summary>
/// The per-test half: leases what the test needs before it runs, and hands it all back after.
/// Derive test classes from this, take the suite's fixture, and build the hosts in
/// <see cref="OnInitializeAsync"/>.
/// </summary>
public abstract class ServiceTestBase(ServiceTestFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
    protected ServiceTestFixture Fixture { get; } = fixture;

    protected ScenarioTestContext Context { get; private set; } = default!;

    protected virtual LogLevel MinimumLogLevel
        => LogLevel.Information;

    public async Task InitializeAsync()
    {
        var logger = LoggerFactory
            .Create(builder => builder.AddProvider(new ScenarioLoggerProvider(output, MinimumLogLevel)))
            .CreateLogger(GetType().Name);

        Context = new ScenarioTestContext(logger);

        await Fixture.Configurator.TestSetupAsync(Context);
        await OnInitializeAsync(Context);
    }

    public async Task DisposeAsync()
    {
        // Throwing here would bury the setup failure that left no context behind.
        if (Context is null)
        {
            return;
        }

        // The hosts go first: they hold the test's database and cache connections open.
        await OnDisposeAsync(Context);
        await Fixture.Configurator.TestTearDownAsync(Context);
    }

    /// <summary>Put the hosts on the context here — one <c>TestWebApplicationFactory</c> per host.</summary>
    protected virtual Task OnInitializeAsync(ScenarioTestContext context)
        => Task.CompletedTask;

    /// <summary>Dispose those hosts here, before their resources are handed back.</summary>
    protected virtual Task OnDisposeAsync(ScenarioTestContext context)
        => Task.CompletedTask;
}
