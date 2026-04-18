using Microsoft.EntityFrameworkCore;
using EconomicDataService.Data; 

var builder = WebApplication.CreateBuilder(args);

// 1. Grab the secure connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("ZarFlowDb");

// 2. Register the Database context with the DI container
builder.Services.AddDbContext<ZarFlowDbContext>(options =>
    options.UseSqlServer(connectionString));
// ----------------------------------

builder.Services.AddOpenApi(); // 10 LTS Standard for API documentation

var app = builder.Build();

// 1. Post: Add new exchange rates to the database (for testing purposes)
app.MapPost("/api/rates", async (ExchangeRate rate, ZarFlowDbContext db) =>
{
    db.ExchangeRates.Add(rate);
    await db.SaveChangesAsync();
    return Results.Created($"/api/rates/{rate.Id}", rate);
});

// 2. GET: Retrieve the latest indicators for the Python MCP Server
app.MapGet("/api/indicators", async (ZarFlowDbContext db) =>
{
    var latestRate = await db.ExchangeRates
        .Where(r => r.BaseCurrency == "USD" && r.TargetCurrency == "ZAR")
        .OrderByDescending(r => r.DateRecorded)
        .FirstOrDefaultAsync();

    
    if (latestRate == null) 
        return Results.NotFound("No exchange rate data found in the database. Please POST some data first.");

    return Results.Ok(new 
    { 
        ZAR_USD = latestRate.Rate, 
        Inflation = 5.3,
        Status = "Live from Azure SQL" 
    });
});

app.Run();