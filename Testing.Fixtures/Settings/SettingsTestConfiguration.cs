using Testing.Fixtures.Core;

namespace Testing.Fixtures.Settings;

/// <summary>
/// Plain host settings, given as a dictionary. Quietening the log and switching off startup
/// migrations are what most suites need.
/// </summary>
public sealed class SettingsTestConfiguration(IDictionary<string, string?> settings) : TestConfiguration
{
    private readonly IDictionary<string, string?> _settings = settings;

    public SettingsTestConfiguration()
        : this(Defaults())
    {
    }

    public static Dictionary<string, string?> Defaults()
        => new()
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
        };

    public override void Configure(ScenarioTestContext context, IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var (key, value) in _settings)
        {
            settings[key] = value;
        }
    }
}
