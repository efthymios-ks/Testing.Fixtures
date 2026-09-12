# Testing.Fixtures

Service tests against the real host: the application in-process, real HTTP, and the infrastructure
it talks to running in containers.

```
Core/TestConfiguration.cs              one piece of the environment — a container, a database, settings
Core/TestConfigurator.cs               runs the pieces: setup in order, teardown in reverse
Core/ServiceTestFixture.cs             the run-scoped half: containers start once
Core/ServiceTestBase.cs                the per-test half: lease, run, hand back
Core/GlobalTestContext.cs              what lives for the whole run
Core/ScenarioTestContext.cs            what one test owns
Core/ScenarioLoggerProvider.cs         host logs, written into the test's output
Core/FixtureDefaults.cs                the parallelism the runner and the pools share
Hosting/TestWebApplicationFactory.cs   a host wired to the test's configurations
Sql/                                   one SQL Server container, a pool of migrated databases
Redis/                                 one Redis container, a numbered database per test
Settings/                              plain host settings
Testing.Fixtures.Host/                 a demo API to test against
Testing.Fixtures.Host.Tests/           its service tests, written with these fixtures
```

## Setting up a suite

```csharp
public sealed class OrdersFixture : ServiceTestFixture
{
    protected override TestConfiguration[] CreateConfigurations() =>
    [
        new SettingsTestConfiguration(),
        new SqlServerTestConfiguration(),
        new SqlDatabaseTestConfiguration<OrdersDbContext>(),
        new RedisTestConfiguration()
    ];
}

[CollectionDefinition(nameof(ServiceTests))]
public sealed class ServiceTests : ICollectionFixture<OrdersFixture>;
```

```csharp
public sealed class OrdersApp(TestConfigurator configurator, ScenarioTestContext context)
    : TestWebApplicationFactory<Program>(configurator, context);

[Collection(nameof(ServiceTests))]
public sealed class GetOrderTests(OrdersFixture fixture, ITestOutputHelper output)
    : ServiceTestBase(fixture, output)
{
    private OrdersApp App => Context.Get<OrdersApp>();
    private OrdersDbContext Database => Context.Get<OrdersDbContext>();

    protected override Task OnInitializeAsync(ScenarioTestContext context)
    {
        context.Set(new OrdersApp(Fixture.Configurator, context));

        return Task.CompletedTask;
    }

    protected override async Task OnDisposeAsync(ScenarioTestContext context)
        => await App.DisposeAsync();

    [Fact]
    public async Task GetOrder_WhenTheOrderExists_ShouldReturnIt()
    {
        // Arrange
        await Database.Orders.AddAsync(new Order { Reference = "A-1" });
        await Database.SaveChangesAsync();

        // Act
        var response = await App.CreateClient().GetAsync("/api/v1/orders/A-1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

The order in `CreateConfigurations` is the start order: the SQL container before the databases that
restore from it. Teardown runs in reverse.

## What the pieces do

| Configuration | Global setup | Per test |
| --- | --- | --- |
| `SettingsTestConfiguration` | — | writes host settings, quiet logging by default |
| `SqlServerTestConfiguration` | starts one SQL Server container | — |
| `SqlDatabaseTestConfiguration<TDbContext>` | migrates a template database and backs it up | leases a database restored from that backup, publishes its connection string and a `TDbContext` |
| `RedisTestConfiguration` | starts one Redis container | leases a numbered database, publishes its connection string |

A database is emptied and put back in the pool rather than dropped, so only the first tests pay for
a restore. Rows a migration seeded are put back on reset, so a test that changes reference data
cannot leak into the next one.

Write your own for anything else — an external API stub, a message transport — by deriving from
`TestConfiguration` and overriding the two setups, the two `Configure` overloads and the two
teardowns you need.

```csharp
public sealed class PaymentApiTestConfiguration : TestConfiguration
{
    public override void Configure(ScenarioTestContext context, IServiceCollection services)
        => services.AddSingleton(Substitute.For<IPaymentApiClient>());
}
```

## Parallelism

One number decides both the runner's parallelism and the size of every pool, so they cannot drift
apart. A pool smaller than the runner's parallelism makes tests queue on a lease, and a lease that
never comes free fails with a timeout after two minutes rather than hanging the run.

```csharp
// In the test assembly, once:
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = FixtureDefaults.MaxParallelThreads)]
```

`FixtureDefaults.MaxParallelThreads` is a const because the attribute needs one. Per-suite overrides
go through the options:

```csharp
new SqlDatabaseTestConfiguration<OrdersDbContext>(new SqlDatabaseFixtureOptions { DatabaseCount = 8 })
```

## The demo host

`Testing.Fixtures.Host` is a shipping-quote API — one endpoint pair, a request and response DTO, an
`IQuoteService` over SQL Server and Redis — and `Testing.Fixtures.Host.Tests` tests it through the
these fixtures. It is what the snippets above look like when they compile.

```
POST /api/v1/quotes            { "destination": "athens", "weightKg": 4 }
                            -> { "reference": "…", "priceEuros": 10.00, "fromCache": false }
GET  /api/v1/quotes/{reference}
```

The rates are seeded by the migration, so every test starts from three destinations and no quotes —
which is also what proves the pool's reset. These tests need a Docker daemon: without one they fail,
because a service test that quietly does not run is worse than a red one.

## License

MIT.
