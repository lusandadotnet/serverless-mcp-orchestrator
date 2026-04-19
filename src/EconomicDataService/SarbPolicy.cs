namespace EconomicDataService;

/// <summary>South African Reserve Bank inflation target band (headline CPI).</summary>
public static class SarbPolicy
{
    public const decimal LowerCpiPercent = 3.0m;
    public const decimal UpperCpiPercent = 6.0m;

    public static bool IsWithinBand(decimal cpiYearOnYear) =>
        cpiYearOnYear >= LowerCpiPercent && cpiYearOnYear <= UpperCpiPercent;
}
