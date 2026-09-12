namespace Testing.Fixtures.Core;

/// <summary>
/// The parallelism a test assembly runs at, told to the fixtures so their pools are sized to match.
/// Declare it next to xUnit's own attribute, from the same constant:
/// <code>
/// [assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = Parallelism.MaxThreads)]
/// [assembly: FixtureParallelism(Parallelism.MaxThreads)]
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class FixtureParallelismAttribute : Attribute
{
    public FixtureParallelismAttribute(int maxParallelThreads)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxParallelThreads, 1);

        MaxParallelThreads = maxParallelThreads;
    }

    public int MaxParallelThreads { get; }
}
