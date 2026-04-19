using EconomicDataService;
using EconomicDataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Resolve provider at DbContext build time so integration tests can inject Testing:InMemoryDatabaseName
// after the host configuration pipeline has merged (WebApplicationFactory).
builder.Services.AddDbContext<ZarFlowDbContext>((sp, options) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var inMemoryDatabaseName = configuration["Testing:InMemoryDatabaseName"];
    if (!string.IsNullOrEmpty(inMemoryDatabaseName))
    {
        options.UseInMemoryDatabase(inMemoryDatabaseName);
        return;
    }

    var connectionString = configuration.GetConnectionString("ZarFlowDb")
        ?? throw new InvalidOperationException("Connection string 'ZarFlowDb' not found. Set User Secrets or the ConnectionStrings__ZarFlowDb environment variable.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ZarFlowDbContext>();
    if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.MigrateAsync();
    else if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

var v1 = app.MapGroup("/api/v1").WithTags("ZAR-Flow v1");

v1.MapGet("/indicators/summary", GetIndicatorsSummaryAsync);
v1.MapGet("/exchange-rates/latest", GetLatestExchangeRateAsync);
v1.MapPost("/exchange-rates", CreateExchangeRateAsync);
v1.MapGet("/inflation/latest", GetLatestInflationAsync);
v1.MapPost("/inflation/readings", CreateInflationReadingAsync);

// Back-compat for MCP clients using the original path
app.MapGet("/api/indicators", GetIndicatorsSummaryAsync);

app.Run();

static async Task<IResult> GetIndicatorsSummaryAsync(ZarFlowDbContext db)
{
    var latestRate = await db.ExchangeRates
        .Where(r => r.BaseCurrency == "USD" && r.TargetCurrency == "ZAR")
        .OrderByDescending(r => r.DateRecorded)
        .FirstOrDefaultAsync();

    var latestInflation = await db.InflationReadings
        .OrderByDescending(r => r.AsOfDate)
        .FirstOrDefaultAsync();

    if (latestRate is null && latestInflation is null)
    {
        return Results.NotFound(new
        {
            message = "No economic data found. POST /api/v1/exchange-rates or /api/v1/inflation/readings first."
        });
    }

    var cpi = latestInflation?.CpiYearOnYear;
    bool? within = cpi is null ? null : SarbPolicy.IsWithinBand(cpi.Value);

    var body = new IndicatorsSummaryResponse(
        ZarUsd: latestRate?.Rate ?? 0m,
        CpiYearOnYear: latestInflation?.CpiYearOnYear ?? 0m,
        ExchangeRateAsOf: latestRate?.DateRecorded,
        InflationAsOf: latestInflation?.AsOfDate,
        WithinSarbBand: within,
        Status: "Live from Azure SQL");

    return Results.Ok(body);
}

static async Task<IResult> GetLatestExchangeRateAsync(string? baseCurrency, string? targetCurrency, ZarFlowDbContext db)
{
    var b = string.IsNullOrWhiteSpace(baseCurrency) ? "USD" : baseCurrency.Trim().ToUpperInvariant();
    var t = string.IsNullOrWhiteSpace(targetCurrency) ? "ZAR" : targetCurrency.Trim().ToUpperInvariant();

    var latest = await db.ExchangeRates
        .Where(r => r.BaseCurrency == b && r.TargetCurrency == t)
        .OrderByDescending(r => r.DateRecorded)
        .FirstOrDefaultAsync();

    if (latest is null)
        return Results.NotFound(new { message = $"No exchange rate for {b}/{t}." });

    return Results.Ok(new ExchangeRateResponse(
        latest.Id,
        latest.BaseCurrency,
        latest.TargetCurrency,
        latest.Rate,
        latest.DateRecorded));
}

static async Task<IResult> CreateExchangeRateAsync(ExchangeRate rate, ZarFlowDbContext db)
{
    if (string.IsNullOrWhiteSpace(rate.BaseCurrency) || string.IsNullOrWhiteSpace(rate.TargetCurrency))
        return Results.BadRequest(new { message = "BaseCurrency and TargetCurrency are required." });

    rate.BaseCurrency = rate.BaseCurrency.Trim().ToUpperInvariant();
    rate.TargetCurrency = rate.TargetCurrency.Trim().ToUpperInvariant();
    rate.Id = 0;

    db.ExchangeRates.Add(rate);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/exchange-rates/latest?baseCurrency={Uri.EscapeDataString(rate.BaseCurrency)}&targetCurrency={Uri.EscapeDataString(rate.TargetCurrency)}", rate);
}

static async Task<IResult> GetLatestInflationAsync(ZarFlowDbContext db)
{
    var latest = await db.InflationReadings
        .OrderByDescending(r => r.AsOfDate)
        .FirstOrDefaultAsync();

    if (latest is null)
        return Results.NotFound(new { message = "No inflation readings stored." });

    return Results.Ok(new InflationReadingResponse(
        latest.Id,
        latest.CpiYearOnYear,
        latest.AsOfDate,
        latest.Source,
        SarbPolicy.IsWithinBand(latest.CpiYearOnYear)));
}

static async Task<IResult> CreateInflationReadingAsync(InflationReadingDto dto, ZarFlowDbContext db)
{
    if (dto.CpiYearOnYear < 0)
        return Results.BadRequest(new { message = "CpiYearOnYear must be non-negative." });

    var entity = new InflationReading
    {
        CpiYearOnYear = dto.CpiYearOnYear,
        AsOfDate = dto.AsOfDate,
        Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source.Trim()
    };

    db.InflationReadings.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/inflation/latest", entity);
}

internal sealed record IndicatorsSummaryResponse(
    decimal ZarUsd,
    decimal CpiYearOnYear,
    DateTime? ExchangeRateAsOf,
    DateTime? InflationAsOf,
    bool? WithinSarbBand,
    string Status);

internal sealed record ExchangeRateResponse(
    int Id,
    string BaseCurrency,
    string TargetCurrency,
    decimal Rate,
    DateTime DateRecorded);

internal sealed record InflationReadingResponse(
    int Id,
    decimal CpiYearOnYear,
    DateTime AsOfDate,
    string? Source,
    bool WithinSarbBand);

internal sealed record InflationReadingDto(decimal CpiYearOnYear, DateTime AsOfDate, string? Source);

public partial class Program { }
