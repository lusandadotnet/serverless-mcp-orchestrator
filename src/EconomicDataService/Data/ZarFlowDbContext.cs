using Microsoft.EntityFrameworkCore;

namespace EconomicDataService.Data;

public class ZarFlowDbContext : DbContext
{
    public ZarFlowDbContext(DbContextOptions<ZarFlowDbContext> options) : base(options) { }

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<InflationReading> InflationReadings => Set<InflationReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRate>(e =>
        {
            e.Property(x => x.Rate).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InflationReading>(e =>
        {
            e.Property(x => x.CpiYearOnYear).HasPrecision(18, 4);
        });
    }
}


public class ExchangeRate
{
    public int Id { get; set; }
    public required string BaseCurrency { get; set; }
    public required string TargetCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime DateRecorded { get; set; }
}
