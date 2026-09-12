using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Testing.Fixtures.Core;

/// <summary>
/// Drives the configurations: setups in the order given, teardowns in reverse. A teardown that
/// throws is reported and the rest still run, so one leak cannot strand a container.
/// </summary>
public sealed class TestConfigurator
{
    private readonly TestConfiguration[] _configurations;

    public TestConfigurator(params TestConfiguration[] configurations)
    {
        ArgumentNullException.ThrowIfNull(configurations);

        _configurations = configurations;
    }

    public IReadOnlyList<TestConfiguration> Configurations
        => _configurations;

    public async Task GlobalSetupAsync(GlobalTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var started = new List<TestConfiguration>(_configurations.Length);

        foreach (var configuration in _configurations)
        {
            try
            {
                await configuration.GlobalSetupAsync(context);
                started.Add(configuration);
            }
            catch
            {
                // What already started has to come down, or the run leaves containers behind.
                await TearDownAsync(started, configuration => configuration.GlobalTearDownAsync(context), logger: null);

                throw;
            }
        }
    }

    public async Task TestSetupAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var configuration in _configurations)
        {
            await configuration.TestSetupAsync(context);
        }
    }

    public void ConfigureWebHost(IWebHostBuilder builder, ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(context);

        var settings = new Dictionary<string, string?>();

        foreach (var configuration in _configurations)
        {
            configuration.Configure(context, settings);
        }

        // Host settings, not ConfigureAppConfiguration: they surface through IConfiguration before
        // the application builds its services, which is when connection strings are read.
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.UseEnvironment(TestEnvironment.Name);

        builder.ConfigureServices(services =>
        {
            foreach (var configuration in _configurations)
            {
                configuration.Configure(context, services);
            }
        });
    }

    public Task TestTearDownAsync(ScenarioTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TearDownAsync(
            _configurations,
            configuration => configuration.TestTearDownAsync(context),
            context.Logger
        );
    }

    public Task GlobalTearDownAsync(GlobalTestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TearDownAsync(_configurations, configuration => configuration.GlobalTearDownAsync(context), logger: null);
    }

    private static async Task TearDownAsync(
        IReadOnlyList<TestConfiguration> configurations,
        Func<TestConfiguration, Task> tearDown,
        ILogger? logger
    )
    {
        for (var index = configurations.Count - 1; index >= 0; index--)
        {
            var configuration = configurations[index];

            try
            {
                await tearDown(configuration);
            }
            catch (Exception exception)
            {
                var message = $"Teardown failed for {configuration.GetType().Name}: {exception.Message}";

                // At run scope there is no test output to write to, so the console it is.
                if (logger is null)
                {
                    Console.Error.WriteLine(message);
                }
                else
                {
                    logger.LogWarning("{Message}", message);
                }
            }
        }
    }
}
