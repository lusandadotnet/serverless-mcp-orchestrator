using System.Net;
using System.Net.Http.Json;
using EconomicDataService.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EconomicDataService.Tests;

public class ZarFlowApiTests
{
    private static ZarFlowWebApplicationFactory CreateFactory() => new();

    [Fact]
    public async Task IndicatorsSummary_ReturnsNotFound_WhenDatabaseEmpty()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/v1/indicators/summary");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task FullPipeline_ExchangeRateAndInflation_ThenSummary_IsOk()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var rateRes = await client.PostAsJsonAsync("/api/v1/exchange-rates", new ExchangeRate
        {
            BaseCurrency = "USD",
            TargetCurrency = "ZAR",
            Rate = 18.42m,
            DateRecorded = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        Assert.Equal(HttpStatusCode.Created, rateRes.StatusCode);

        var infRes = await client.PostAsJsonAsync("/api/v1/inflation/readings", new
        {
            cpiYearOnYear = 5.3m,
            asOfDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc),
            source = "UnitTest"
        });
        Assert.Equal(HttpStatusCode.Created, infRes.StatusCode);

        var summary = await client.GetFromJsonAsync<IndicatorsSummaryJson>("/api/v1/indicators/summary");

        Assert.NotNull(summary);
        Assert.Equal(18.42m, summary.ZarUsd);
        Assert.Equal(5.3m, summary.CpiYearOnYear);
        Assert.True(summary.WithinSarbBand);
        Assert.Equal("Live from Azure SQL", summary.Status);
    }

    [Fact]
    public async Task LegacyPath_ApiIndicators_MatchesV1Summary()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/exchange-rates", new ExchangeRate
        {
            BaseCurrency = "USD",
            TargetCurrency = "ZAR",
            Rate = 19m,
            DateRecorded = DateTime.UtcNow
        });
        await client.PostAsJsonAsync("/api/v1/inflation/readings", new
        {
            cpiYearOnYear = 4.5m,
            asOfDate = DateTime.UtcNow,
            source = (string?)null
        });

        var a = await client.GetFromJsonAsync<IndicatorsSummaryJson>("/api/indicators");
        var b = await client.GetFromJsonAsync<IndicatorsSummaryJson>("/api/v1/indicators/summary");

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a.ZarUsd, b.ZarUsd);
        Assert.Equal(a.CpiYearOnYear, b.CpiYearOnYear);
    }

    private sealed record IndicatorsSummaryJson(
        decimal ZarUsd,
        decimal CpiYearOnYear,
        bool? WithinSarbBand,
        string Status);
}
