namespace Reflect.Domain.Rules;

/// <summary>
/// Thresholds for Remote Therapeutic Monitoring (CPT 2026). Kept in one place so a billing or
/// compliance reviewer can change them without touching the calculator.
/// </summary>
public sealed record RtmRules
{
    /// <summary>Device-supply codes are reported per 30-day period.</summary>
    public int PeriodLengthDays { get; init; } = 30;

    /// <summary>Minimum data days for the 2-15 day device-supply code (98986).</summary>
    public int ShortSupplyMinDays { get; init; } = 2;

    /// <summary>Minimum data days for the 16-30 day device-supply code (98978).</summary>
    public int FullSupplyMinDays { get; init; } = 16;

    /// <summary>Minimum management minutes in a calendar month for 98979 (10-19 minutes).</summary>
    public int BriefManagementMinutes { get; init; } = 10;

    /// <summary>Minimum management minutes in a calendar month for 98980.</summary>
    public int FullManagementMinutes { get; init; } = 20;

    /// <summary>Size of each additional block reported with 98981.</summary>
    public int AdditionalBlockMinutes { get; init; } = 20;

    public static RtmRules Default { get; } = new();
}
