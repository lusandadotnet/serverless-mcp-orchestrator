using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace EconomicDataService.Tests;

public class IndicatorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IndicatorTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetIndicators_ReturnsMockSarbData()
    {
        // Act
        var response = await _client.GetAsync("/api/indicators");
        var data = await response.Content.ReadFromJsonAsync<dynamic>();

        [cite_start]// Assert - Validating Roadmap Phase 1 requirements 
        Assert.NotNull(data);
        // Using 'ToString' here for quick dynamic checking of the JSON properties
        Assert.Contains("18.45", data?.ToString()); 
        Assert.Contains("5.3", data?.ToString());
    }
}