using Microsoft.EntityFrameworkCore;

namespace EconomicDataService.Data;


public class ZarFlowDbContext : DbContext
{
    public ZarFlowDbContext(DbContextOptions<ZarFlowDbContext> options) : base(options) { }

    
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
}


public class ExchangeRate
{
    public int Id { get; set; }
    public required string BaseCurrency { get; set; }
    public required string TargetCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime DateRecorded { get; set; }
}
