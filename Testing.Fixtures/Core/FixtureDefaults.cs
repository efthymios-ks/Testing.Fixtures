using System.Reflection;

namespace Testing.Fixtures.Core;

/// <summary>
/// The one number the runner and the pools have to agree on. A test assembly states it with
/// <see cref="FixtureParallelismAttribute"/>, and every pool sizes itself from that.
/// </summary>
public static class FixtureDefaults
{
    /// <summary>
    /// What a pool uses when no assembly says otherwise. A const, because xUnit's
    /// <c>CollectionBehavior</c> attribute takes one.
    /// </summary>
    public const int DefaultMaxParallelThreads = 16;

    private static readonly Lazy<int> _maxParallelThreads = new(ResolveMaxParallelThreads);

    /// <summary>Tests running at once, and the size of every resource pool that serves them.</summary>
    public static int MaxParallelThreads
        => _maxParallelThreads.Value;

    /// <summary>How long a test waits for a pooled resource before failing rather than hanging.</summary>
    public static TimeSpan LeaseTimeout { get; } = TimeSpan.FromMinutes(2);

    private static int ResolveMaxParallelThreads()
        => AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetCustomAttribute<FixtureParallelismAttribute>())
            .FirstOrDefault(attribute => attribute is not null)
            ?.MaxParallelThreads
            ?? DefaultMaxParallelThreads;
}
