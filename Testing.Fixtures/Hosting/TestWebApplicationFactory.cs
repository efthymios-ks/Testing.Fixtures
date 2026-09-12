using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testing.Fixtures.Core;

namespace Testing.Fixtures.Hosting;

/// <summary>
/// A host under test, wired to the test's configurations. Derive one per host —
/// <c>public sealed class OrdersApp(TestConfigurator configurator, ScenarioTestContext context)
/// : TestWebApplicationFactory&lt;Program&gt;(configurator, context);</c>
/// </summary>
public class TestWebApplicationFactory<TEntryPoint>(TestConfigurator configurator, ScenarioTestContext context)
    : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    protected ScenarioTestContext Context { get; } = context;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => configurator.ConfigureWebHost(builder, Context);
}
