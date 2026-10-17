namespace Reflect.Domain.Rules;

/// <summary>RTM CPT codes relevant to mental-health (CBT-category) monitoring, 2026 code set.</summary>
public static class RtmCodes
{
    public const string Setup = "98975";
    public const string DeviceSupplyFull = "98978";
    public const string DeviceSupplyShort = "98986";
    public const string ManagementBrief = "98979";
    public const string ManagementFull = "98980";
    public const string ManagementAdditional = "98981";

    public static string Describe(string code) => code switch
    {
        Setup => "Initial set-up and patient education on the monitoring app",
        DeviceSupplyFull => "CBT monitoring device supply, 16-30 data days in a 30-day period",
        DeviceSupplyShort => "CBT monitoring device supply, 2-15 data days in a 30-day period",
        ManagementBrief => "Treatment management, first 10-19 minutes in a calendar month",
        ManagementFull => "Treatment management, first 20 minutes in a calendar month",
        ManagementAdditional => "Treatment management, each additional 20 minutes",
        _ => code
    };
}
