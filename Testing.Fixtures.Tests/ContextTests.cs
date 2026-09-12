using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testing.Fixtures.Core;
using Xunit.Abstractions;

namespace Testing.Fixtures.Tests;

public sealed class ContextTests
{
    [Fact]
    public void GlobalContext_WhenAValueIsSet_ShouldHandItBackByType()
    {
        // Arrange
        var context = new GlobalTestContext();

        // Act
        context.Set(new Marker("redis"));

        // Assert
        Assert.Equal("redis", context.Get<Marker>().Name);
    }

    [Fact]
    public void GlobalContext_WhenTheValueIsMissing_ShouldThrowSayingWhichOne()
    {
        // Arrange
        var context = new GlobalTestContext();

        // Act
        var error = Assert.Throws<InvalidOperationException>(context.Get<Marker>);

        // Assert
        Assert.Contains(nameof(Marker), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalContext_WhenTheValueIsMissing_ShouldReportItRatherThanThrow()
    {
        // Arrange
        var context = new GlobalTestContext();

        // Act
        var found = context.TryGet<Marker>(out var value);

        // Assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void GlobalContext_WhenTheValueIsSetTwice_ShouldKeepTheLast()
    {
        // Arrange
        var context = new GlobalTestContext();

        // Act
        context.Set(new Marker("first"));
        context.Set(new Marker("second"));

        // Assert
        Assert.Equal("second", context.Get<Marker>().Name);
    }

    [Fact]
    public void ScenarioContext_WhenAValueIsSet_ShouldHandItBackByType()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.Set(new Marker("database"));

        // Assert
        Assert.Equal("database", context.Get<Marker>().Name);
    }

    [Fact]
    public void ScenarioContext_WhenAValueIsKeyed_ShouldKeepItApartFromTheTypedOne()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.Set(new Marker("typed"));
        context.Set(new Marker("keyed"), "keyed");

        // Assert
        Assert.Equal("typed", context.Get<Marker>().Name);
        Assert.Equal("keyed", context.Get<Marker>("keyed").Name);
    }

    [Fact]
    public void ScenarioContext_WhenTheValueIsMissing_ShouldThrowSayingWhichOne()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var error = Assert.Throws<InvalidOperationException>(context.Get<Marker>);

        // Assert
        Assert.Contains(nameof(Marker), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioContext_WhenTheValueIsMissing_ShouldReportItRatherThanThrow()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.False(context.TryGet<Marker>(out _));
        Assert.False(context.TryGet<Marker>("keyed", out _));
    }

    [Fact]
    public void ScenarioContext_WhenTheKeyIsEmpty_ShouldThrow()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Set(new Marker("value"), " "));
    }

    [Fact]
    public void ScenarioId_WhenTwoContextsAreBuilt_ShouldDiffer()
    {
        // Arrange
        var first = CreateContext();
        var second = CreateContext();

        // Act & Assert
        Assert.NotEqual(first.ScenarioId, second.ScenarioId);
        Assert.NotEmpty(first.ScenarioId);
    }

    [Fact]
    public void Logger_WhenTheLevelIsBelowTheMinimum_ShouldWriteNothing()
    {
        // Arrange
        var output = new RecordingOutput();
        using var provider = new ScenarioLoggerProvider(output, LogLevel.Warning);
        var logger = provider.CreateLogger("category");

        // Act
        logger.LogInformation("quiet");
        logger.LogWarning("loud");

        // Assert
        Assert.Single(output.Lines);
        Assert.Contains("loud", output.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Logger_WhenTheOutputIsClosed_ShouldNotThrow()
    {
        // Arrange
        var output = new RecordingOutput { Closed = true };
        using var provider = new ScenarioLoggerProvider(output);
        var logger = provider.CreateLogger("category");

        // Act
        logger.LogInformation("after the test");

        // Assert
        Assert.Empty(output.Lines);
    }

    [Fact]
    public void Logger_WhenWriting_ShouldCarryTheLevelAndTheCategory()
    {
        // Arrange
        var output = new RecordingOutput();
        using var provider = new ScenarioLoggerProvider(output);
        var logger = provider.CreateLogger("Orders");

        // Act
        logger.LogInformation("started");

        // Assert
        Assert.Equal("[Information] Orders: started", output.Lines[0]);
    }

    private static ScenarioTestContext CreateContext()
        => new(NullLogger.Instance);

    private sealed record Marker(string Name);

    private sealed class RecordingOutput : ITestOutputHelper
    {
        public List<string> Lines { get; } = [];

        public bool Closed { get; init; }

        public void WriteLine(string message)
        {
            if (Closed)
            {
                throw new InvalidOperationException("There is no currently active test.");
            }

            Lines.Add(message);
        }

        public void WriteLine(string format, params object[] args)
            => WriteLine(string.Format(format, args));
    }
}
