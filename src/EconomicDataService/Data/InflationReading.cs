namespace EconomicDataService.Data;

public class InflationReading
{
    public int Id { get; set; }

    /// <summary>Headline CPI year-on-year, percent.</summary>
    public decimal CpiYearOnYear { get; set; }

    public DateTime AsOfDate { get; set; }

    public string? Source { get; set; }
}
