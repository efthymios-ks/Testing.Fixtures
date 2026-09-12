namespace Testing.Fixtures.Sql;

public sealed class SqlServerFixtureOptions
{
    /// <summary>Pinned rather than latest, so a run is repeatable.</summary>
    public string Image { get; set; } = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
}
