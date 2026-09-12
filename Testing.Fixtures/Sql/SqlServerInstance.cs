namespace Testing.Fixtures.Sql;

/// <summary>The running SQL Server container, addressed through its master connection string.</summary>
public sealed record SqlServerInstance(string MasterConnectionString);
