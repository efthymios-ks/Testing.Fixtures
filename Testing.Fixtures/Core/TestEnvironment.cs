namespace Testing.Fixtures.Core;

/// <summary>
/// The environment name the host runs under in a service test. Hosts read it to pick the test
/// transport and configuration, so it has to be the same string on both sides.
/// </summary>
public static class TestEnvironment
{
    public const string Name = "ServiceTest";
}
