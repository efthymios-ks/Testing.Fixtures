using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testing.Fixtures.Core;

namespace Testing.Fixtures.Tests;

public sealed class TestConfiguratorTests
{
    [Fact]
    public async Task GlobalSetupAsync_WhenCalled_ShouldRunTheConfigurationsInOrder()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log), new RecordingConfiguration("second", log));

        // Act
        await configurator.GlobalSetupAsync(new GlobalTestContext());

        // Assert
        Assert.Equal(["first:global-setup", "second:global-setup"], log);
    }

    [Fact]
    public async Task GlobalTearDownAsync_WhenCalled_ShouldRunTheConfigurationsInReverse()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log), new RecordingConfiguration("second", log));

        // Act
        await configurator.GlobalTearDownAsync(new GlobalTestContext());

        // Assert
        Assert.Equal(["second:global-teardown", "first:global-teardown"], log);
    }

    [Fact]
    public async Task GlobalSetupAsync_WhenOneFails_ShouldTearDownWhatAlreadyStarted()
    {
        // Arrange
        var log = new List<string>();

        var configurator = new TestConfigurator(
            new RecordingConfiguration("first", log),
            new RecordingConfiguration("second", log) { FailGlobalSetup = true },
            new RecordingConfiguration("third", log)
        );

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => configurator.GlobalSetupAsync(new GlobalTestContext()));

        // Assert
        Assert.Equal(["first:global-setup", "second:global-setup", "first:global-teardown"], log);
    }

    [Fact]
    public async Task TestSetupAsync_WhenCalled_ShouldRunTheConfigurationsInOrder()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log), new RecordingConfiguration("second", log));

        // Act
        await configurator.TestSetupAsync(CreateContext());

        // Assert
        Assert.Equal(["first:test-setup", "second:test-setup"], log);
    }

    [Fact]
    public async Task TestTearDownAsync_WhenCalled_ShouldRunTheConfigurationsInReverse()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log), new RecordingConfiguration("second", log));

        // Act
        await configurator.TestTearDownAsync(CreateContext());

        // Assert
        Assert.Equal(["second:test-teardown", "first:test-teardown"], log);
    }

    [Fact]
    public async Task TestTearDownAsync_WhenOneThrows_ShouldStillRunTheRest()
    {
        // Arrange
        var log = new List<string>();

        var configurator = new TestConfigurator(
            new RecordingConfiguration("first", log),
            new RecordingConfiguration("second", log) { FailTestTearDown = true }
        );

        // Act
        await configurator.TestTearDownAsync(CreateContext());

        // Assert
        Assert.Equal(["second:test-teardown", "first:test-teardown"], log);
    }

    [Fact]
    public async Task GlobalTearDownAsync_WhenOneThrows_ShouldStillRunTheRest()
    {
        // Arrange
        var log = new List<string>();

        var configurator = new TestConfigurator(
            new RecordingConfiguration("first", log),
            new RecordingConfiguration("second", log) { FailGlobalTearDown = true }
        );

        // Act
        await configurator.GlobalTearDownAsync(new GlobalTestContext());

        // Assert
        Assert.Equal(["second:global-teardown", "first:global-teardown"], log);
    }

    [Fact]
    public void ConfigureWebHost_WhenCalled_ShouldApplyTheSettingsOfEveryConfiguration()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log), new RecordingConfiguration("second", log));
        var builder = new WebHostBuilder();

        // Act
        configurator.ConfigureWebHost(builder, CreateContext());

        // Assert
        Assert.Equal("first", builder.GetSetting("first"));
        Assert.Equal("second", builder.GetSetting("second"));
    }

    [Fact]
    public void ConfigureWebHost_WhenCalled_ShouldRunUnderTheTestEnvironment()
    {
        // Arrange
        var configurator = new TestConfigurator();
        var builder = new WebHostBuilder();

        // Act
        configurator.ConfigureWebHost(builder, CreateContext());

        // Assert
        Assert.Equal(TestEnvironment.Name, builder.GetSetting(WebHostDefaults.EnvironmentKey));
    }

    [Fact]
    public void ConfigureWebHost_WhenTheHostIsBuilt_ShouldRegisterTheServicesOfEveryConfiguration()
    {
        // Arrange
        var log = new List<string>();
        var configurator = new TestConfigurator(new RecordingConfiguration("first", log));

        var builder = new WebHostBuilder()
            .UseServer(new StubServer())
            .Configure(_ => { });

        // Act
        configurator.ConfigureWebHost(builder, CreateContext());
        builder.Build().Dispose();

        // Assert
        Assert.Contains("first:services", log);
    }

    [Fact]
    public void Configurations_WhenNoneAreGiven_ShouldBeEmpty()
    {
        // Arrange
        var configurator = new TestConfigurator();

        // Act & Assert
        Assert.Empty(configurator.Configurations);
    }

    [Fact]
    public void Constructor_WhenTheConfigurationsAreNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestConfigurator(null!));
    }

    [Fact]
    public async Task Methods_WhenTheContextIsNull_ShouldThrow()
    {
        // Arrange
        var configurator = new TestConfigurator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => configurator.GlobalSetupAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => configurator.TestSetupAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => configurator.TestTearDownAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => configurator.GlobalTearDownAsync(null!));
        Assert.Throws<ArgumentNullException>(() => configurator.ConfigureWebHost(null!, CreateContext()));
    }

    private static ScenarioTestContext CreateContext()
        => new(NullLogger.Instance);

    /// <summary>A host needs a server to build; this one listens to nothing.</summary>
    private sealed class StubServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class RecordingConfiguration(string name, List<string> log) : TestConfiguration
    {
        public bool FailGlobalSetup { get; init; }

        public bool FailTestTearDown { get; init; }

        public bool FailGlobalTearDown { get; init; }

        public override Task GlobalSetupAsync(GlobalTestContext context)
        {
            log.Add($"{name}:global-setup");

            return FailGlobalSetup
                ? throw new InvalidOperationException($"{name} failed")
                : Task.CompletedTask;
        }

        public override Task TestSetupAsync(ScenarioTestContext context)
        {
            log.Add($"{name}:test-setup");

            return Task.CompletedTask;
        }

        public override void Configure(ScenarioTestContext context, IDictionary<string, string?> settings)
            => settings[name] = name;

        public override void Configure(ScenarioTestContext context, IServiceCollection services)
            => log.Add($"{name}:services");

        public override Task TestTearDownAsync(ScenarioTestContext context)
        {
            log.Add($"{name}:test-teardown");

            return FailTestTearDown
                ? throw new InvalidOperationException($"{name} failed")
                : Task.CompletedTask;
        }

        public override Task GlobalTearDownAsync(GlobalTestContext context)
        {
            log.Add($"{name}:global-teardown");

            return FailGlobalTearDown
                ? throw new InvalidOperationException($"{name} failed")
                : Task.CompletedTask;
        }
    }
}
