using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Testing.Fixtures.Core;
using Testing.Fixtures.Host.Tests.Core;
using Testing.Fixtures.Host.Features.Persistence;
using Testing.Fixtures.Host.Features.Quotes;
using Xunit.Abstractions;

namespace Testing.Fixtures.Host.Tests.Features.Quotes;

[Collection(nameof(QuotesServiceTests))]
public sealed class QuoteTests(QuotesFixture fixture, ITestOutputHelper output)
    : ServiceTestBase(fixture, output)
{
    private QuotesApp App
        => Context.Get<QuotesApp>();

    private QuotesDbContext Database
        => Context.Get<QuotesDbContext>();

    [Fact]
    public async Task Quote_WhenTheDestinationIsPriced_ShouldReturnThePriceAndWriteItDown()
    {
        // Arrange
        using var client = App.CreateClient();
        var request = new { Destination = "athens", WeightKg = 4m };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/quotes", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var quote = (await response.Content.ReadFromJsonAsync<QuoteResponse>())!;

        Assert.Equal(10.00m, quote.PriceEuros);
        Assert.False(quote.FromCache);
        Assert.True(await Database.Quotes.AnyAsync(entity => entity.Reference == quote.Reference));
    }

    [Fact]
    public async Task Quote_WhenTheSameParcelIsPricedTwice_ShouldComeBackFromTheCache()
    {
        // Arrange
        using var client = App.CreateClient();
        var request = new { Destination = "berlin", WeightKg = 2m };

        // Act
        using var first = await client.PostAsJsonAsync("/api/v1/quotes", request);
        using var second = await client.PostAsJsonAsync("/api/v1/quotes", request);

        // Assert
        var cached = (await second.Content.ReadFromJsonAsync<QuoteResponse>())!;

        Assert.False((await first.Content.ReadFromJsonAsync<QuoteResponse>())!.FromCache);
        Assert.True(cached.FromCache);
        Assert.Equal(1, await Database.Quotes.CountAsync());
    }

    [Fact]
    public async Task Quote_WhenNothingShipsThere_ShouldReturnNotFound()
    {
        // Arrange
        using var client = App.CreateClient();
        var request = new { Destination = "atlantis", WeightKg = 1m };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/quotes", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await Database.Quotes.CountAsync());
    }

    [Fact]
    public async Task Quote_WhenTheWeightIsNotPositive_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = App.CreateClient();
        var request = new { Destination = "athens", WeightKg = 0m };

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/quotes", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetQuote_WhenItWasQuoted_ShouldReadItBack()
    {
        // Arrange
        using var client = App.CreateClient();
        using var created = await client.PostAsJsonAsync("/api/v1/quotes", new { Destination = "london", WeightKg = 3m });
        var reference = (await created.Content.ReadFromJsonAsync<QuoteResponse>())!.Reference;

        // Act
        using var response = await client.GetAsync($"/api/v1/quotes/{reference}");

        // Assert
        var quote = (await response.Content.ReadFromJsonAsync<QuoteResponse>())!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(15.60m, quote.PriceEuros);
    }

    [Fact]
    public async Task GetQuote_WhenTheReferenceIsUnknown_ShouldReturnNotFound()
    {
        // Arrange
        using var client = App.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/quotes/nothing");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Database_WhenATestStarts_ShouldCarryTheSeededRatesAndNothingElse()
    {
        // Arrange
        var rates = await Database.ShippingRates.CountAsync();

        // Act
        var quotes = await Database.Quotes.CountAsync();

        // Assert
        Assert.Equal(3, rates);
        Assert.Equal(0, quotes);
    }

    [Fact]
    public async Task Cache_WhenATestStarts_ShouldNotSeeAnotherTestsQuote()
    {
        // Arrange
        using var client = App.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/quotes", new { Destination = "athens", WeightKg = 4m });

        // Assert
        Assert.False((await response.Content.ReadFromJsonAsync<QuoteResponse>())!.FromCache);
    }

    protected override Task OnInitializeAsync(ScenarioTestContext context)
    {
        context.Set(new QuotesApp(Fixture.Configurator, context));

        return Task.CompletedTask;
    }

    protected override async Task OnDisposeAsync(ScenarioTestContext context)
    {
        if (context.TryGet<QuotesApp>(out var app))
        {
            await app.DisposeAsync();
        }
    }
}
