using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DbmsComparison.Api.Tests;

public class DatabaseIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DatabaseIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Providers_ReturnsSupportedList()
    {
        using var response = await _client.GetAsync("/api/database/providers");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var supported = payload.GetProperty("supported").EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Contains("sqlserver", supported);
        Assert.Contains("postgres", supported);
        Assert.Contains("mysql", supported);
        Assert.Contains("sqlite", supported);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    [InlineData("mysql")]
    [InlineData("sqlite")]
    public async Task DatabaseTest_ReturnsConnectivityForProvider(string db)
    {
        using var response = await _client.GetAsync($"/api/database/test?db={db}");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var canConnect = payload.GetProperty("canConnect").GetBoolean();
        if (!canConnect)
        {
            return;
        }

        var providerName = payload.GetProperty("provider").GetString();
        Assert.False(string.IsNullOrWhiteSpace(providerName));
    }

    [Fact]
    public async Task ImplementationCost_ReturnsProviders()
    {
        using var response = await _client.GetAsync("/api/database/implementation-cost");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var providers = payload.GetProperty("providers").EnumerateArray().ToList();
        var keys = providers.Select(x => x.GetProperty("key").GetString()).ToList();

        Assert.Contains("sqlserver", keys);
        Assert.Contains("postgres", keys);
        Assert.Contains("mysql", keys);
        Assert.Contains("sqlite", keys);
    }
}
