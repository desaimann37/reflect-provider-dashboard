namespace Reflect.Domain.Clinical;

/// <summary>Observation codes used by the Provider area. LOINC where a standard code exists.</summary>
public static class ObservationCodes
{
    public const string Loinc = "http://loinc.org";
    public const string Reflect = "urn:reflect:observation";

    /// <summary>PHQ-9 total score (0-27).</summary>
    public const string Phq9Total = "44261-6";

    /// <summary>PHQ-9 item 9: thoughts of being better off dead or of self-harm (0-3).</summary>
    public const string Phq9Item9 = "44260-8";

    /// <summary>GAD-7 total score (0-21).</summary>
    public const string Gad7Total = "70274-6";

    /// <summary>In-app mood check-in before a session (1-10, higher is better).</summary>
    public const string MoodPre = "reflect-mood-pre";

    /// <summary>In-app mood check-in after a session (1-10, higher is better).</summary>
    public const string MoodPost = "reflect-mood-post";

    public static string DisplayFor(string code) => code switch
    {
        Phq9Total => "PHQ-9 total",
        Phq9Item9 => "PHQ-9 item 9",
        Gad7Total => "GAD-7 total",
        MoodPre => "Mood before session",
        MoodPost => "Mood after session",
        _ => code
    };

    public static string SystemFor(string code) => code switch
    {
        Phq9Total or Phq9Item9 or Gad7Total => Loinc,
        _ => Reflect
    };

    public static bool IsStandardizedMeasure(string code) => code is Phq9Total or Gad7Total;
}
