namespace Testing.Fixtures.Redis;

/// <summary>The running Redis container, for anything that needs it outside a test's lease.</summary>
public sealed record RedisInstance(string ConnectionString);
