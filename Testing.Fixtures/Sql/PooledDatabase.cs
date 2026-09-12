namespace Testing.Fixtures.Sql;

/// <summary>One database out of the pool, held for the length of a test.</summary>
public sealed record PooledDatabase(string ConnectionString, string DatabaseName);
