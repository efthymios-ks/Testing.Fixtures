using Testing.Fixtures.Host.Features.Caching;
using Testing.Fixtures.Host.Features.Persistence;
using Testing.Fixtures.Host.Features.Quotes;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddCaching(builder.Configuration)
    .AddQuotes();

var app = builder.Build();

app.MapQuotes();

await app.RunAsync();

/// <summary>Named so a service test can reach the host through WebApplicationFactory.</summary>
public partial class Program;
