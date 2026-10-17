namespace Reflect.Domain.Rules;

public enum SeverityBand
{
    Minimal = 0,
    Mild = 1,
    Moderate = 2,
    ModeratelySevere = 3,
    Severe = 4
}

/// <summary>Published severity bands for the standardized measures (Kroenke 2001, Spitzer 2006).</summary>
public static class MeasureSeverity
{
    /// <summary>Change in total score generally treated as clinically meaningful.</summary>
    public const int MeaningfulChange = 5;

    public static SeverityBand Phq9(decimal score) => score switch
    {
        <= 4 => SeverityBand.Minimal,
        <= 9 => SeverityBand.Mild,
        <= 14 => SeverityBand.Moderate,
        <= 19 => SeverityBand.ModeratelySevere,
        _ => SeverityBand.Severe
    };

    public static SeverityBand Gad7(decimal score) => score switch
    {
        <= 4 => SeverityBand.Minimal,
        <= 9 => SeverityBand.Mild,
        <= 14 => SeverityBand.Moderate,
        _ => SeverityBand.Severe
    };

    public static string Label(SeverityBand band) => band switch
    {
        SeverityBand.Minimal => "Minimal",
        SeverityBand.Mild => "Mild",
        SeverityBand.Moderate => "Moderate",
        SeverityBand.ModeratelySevere => "Moderately severe",
        _ => "Severe"
    };

    public static string Item9Label(decimal value) => value switch
    {
        <= 0 => "Not at all",
        1 => "Several days",
        2 => "More than half the days",
        _ => "Nearly every day"
    };
}
