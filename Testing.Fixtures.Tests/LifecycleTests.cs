using Testing.Fixtures.Core;
using Xunit.Abstractions;

namespace Testing.Fixtures.Tests;

public sealed class LifecycleTests
{
    [Fact]
    public async Task Fixture_WhenInitialised_ShouldSetUpItsConfigurationsOnce()
    {
        // Arrange
        var log = new List<string>();
        var fixture = new StubFixture(log);

        // Act
        await fixture.InitializeAsync();
        await fixture.DisposeAsync();

        // Assert
        Assert.Equal(["global-setup", "global-teardown"], log);
    }

    [Fact]
    public async Task Fixture_WhenAskedTwice_ShouldHandBackTheSameConfigurator()
    {
        // Arrange
        var fixture = new StubFixture([]);

        // Act
        var first = fixture.Configurator;
        var second = fixture.Configurator;

        // Assert
        Assert.Same(first, second);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Test_WhenRun_ShouldLeaseBeforeTheHostsAndHandBackAfterThem()
    {
        // Arrange
        var log = new List<string>();
        var fixture = new StubFixture(log);
        var test = new StubTest(fixture, log);

        // Act
        await test.InitializeAsync();
        await test.DisposeAsync();

        // Assert
        Assert.Equal(["test-setup", "hosts-up", "hosts-down", "test-teardown"], log);
    }

    [Fact]
    public async Task Test_WhenRun_ShouldGiveItAContextOfItsOwn()
    {
        // Arrange
        var fixture = new StubFixture([]);
        var first = new StubTest(fixture, []);
        var second = new StubTest(fixture, []);

        // Act
        await first.InitializeAsync();
        await second.InitializeAsync();

        // Assert
        Assert.NotEqual(first.ScenarioId, second.ScenarioId);
    }

    [Fact]
    public async Task Test_WhenSetupNeverRan_ShouldTearDownWithoutThrowing()
    {
        // Arrange
        var log = new List<string>();
        var test = new StubTest(new StubFixture(log), log);

        // Act
        await test.DisposeAsync();

        // Assert
        Assert.Empty(log);
    }

    private sealed class StubFixture(List<string> log) : ServiceTestFixture
    {
        protected override TestConfiguration[] CreateConfigurations()
            => [new RecordingConfiguration(log)];
    }

    private sealed class StubTest(ServiceTestFixture fixture, List<string> log)
        : ServiceTestBase(fixture, new NullOutput())
    {
        public string ScenarioId
            => Context.ScenarioId;

        protected override Task OnInitializeAsync(ScenarioTestContext context)
        {
            log.Add("hosts-up");

            return Task.CompletedTask;
        }

        protected override Task OnDisposeAsync(ScenarioTestContext context)
        {
            log.Add("hosts-down");

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingConfiguration(List<string> log) : TestConfiguration
    {
        public override Task GlobalSetupAsync(GlobalTestContext context)
        {
            log.Add("global-setup");

            return Task.CompletedTask;
        }

        public override Task TestSetupAsync(ScenarioTestContext context)
        {
            log.Add("test-setup");

            return Task.CompletedTask;
        }

        public override Task TestTearDownAsync(ScenarioTestContext context)
        {
            log.Add("test-teardown");

            return Task.CompletedTask;
        }

        public override Task GlobalTearDownAsync(GlobalTestContext context)
        {
            log.Add("global-teardown");

            return Task.CompletedTask;
        }
    }

    private sealed class NullOutput : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
        }

        public void WriteLine(string format, params object[] args)
        {
        }
    }
}
