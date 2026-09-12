using Xunit;

namespace Testing.Fixtures.Core;

/// <summary>
/// The run-scoped half: containers start once, before the first test, and stop after the last.
/// Derive one per suite, state its configurations, and share it through a collection fixture:
/// <c>[CollectionDefinition(nameof(ServiceTests))] public sealed class ServiceTests
/// : ICollectionFixture&lt;OrdersFixture&gt;;</c>
/// </summary>
public abstract class ServiceTestFixture : IAsyncLifetime
{
    private TestConfigurator? _configurator;

    public GlobalTestContext Global { get; } = new();

    public TestConfigurator Configurator
        => _configurator ??= new TestConfigurator(CreateConfigurations());

    public async Task InitializeAsync()
        => await Configurator.GlobalSetupAsync(Global);

    public async Task DisposeAsync()
        => await Configurator.GlobalTearDownAsync(Global);

    /// <summary>
    /// The pieces of the environment, in start order — the SQL container before the databases that
    /// restore from it, and so on.
    /// </summary>
    protected abstract TestConfiguration[] CreateConfigurations();
}
