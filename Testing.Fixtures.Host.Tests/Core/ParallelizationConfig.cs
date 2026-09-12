using Testing.Fixtures.Core;
using Testing.Fixtures.Host.Tests.Core;

// One number: xUnit runs this many tests at once, and the fixtures size their pools to match.
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = Parallelism.MaxThreads)]
[assembly: FixtureParallelism(Parallelism.MaxThreads)]

namespace Testing.Fixtures.Host.Tests.Core;

public static class Parallelism
{
    public const int MaxThreads = FixtureDefaults.DefaultMaxParallelThreads;
}
