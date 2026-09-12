using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Testing.Fixtures.Core;

/// <summary>Sends log lines to the test's output, so a failure reads next to the test that hit it.</summary>
public sealed class ScenarioLoggerProvider(ITestOutputHelper output, LogLevel minimumLevel = LogLevel.Information)
    : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new ScenarioLogger(output, categoryName, minimumLevel);

    public void Dispose()
    {
    }

    private sealed class ScenarioLogger(ITestOutputHelper output, string category, LogLevel minimumLevel) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= minimumLevel && logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);

            try
            {
                output.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");
            }
            catch (InvalidOperationException)
            {
                // The output helper only accepts writes while its test runs.
            }
        }
    }
}
