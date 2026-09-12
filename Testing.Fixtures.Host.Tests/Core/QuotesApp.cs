using Testing.Fixtures.Core;
using Testing.Fixtures.Hosting;

namespace Testing.Fixtures.Host.Tests.Core;

/// <summary>The quotes host, wired to one test's database and cache.</summary>
public sealed class QuotesApp(TestConfigurator configurator, ScenarioTestContext context)
    : TestWebApplicationFactory<Program>(configurator, context);
