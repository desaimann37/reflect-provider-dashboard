namespace Reflect.Domain.Clinical;

public sealed record DiagnosisOption(string Code, string Description);

/// <summary>ICD-10-CM codes offered when editing a care plan. Reference data, not patient data.</summary>
public static class DiagnosisCatalog
{
    public static IReadOnlyList<DiagnosisOption> All { get; } = new[]
    {
        new DiagnosisOption("F41.1", "Generalized anxiety disorder"),
        new DiagnosisOption("F41.0", "Panic disorder"),
        new DiagnosisOption("F32.1", "Major depressive disorder, single episode, moderate"),
        new DiagnosisOption("F33.1", "Major depressive disorder, recurrent, moderate"),
        new DiagnosisOption("F43.10", "Post-traumatic stress disorder, unspecified"),
        new DiagnosisOption("F43.12", "Post-traumatic stress disorder, chronic"),
        new DiagnosisOption("F43.23", "Adjustment disorder with mixed anxiety and depressed mood"),
        new DiagnosisOption("F40.10", "Social phobia, unspecified")
    };

    public static IReadOnlyList<string> Programs { get; } = new[]
    {
        "Anxiety care",
        "Mood care",
        "Trauma recovery",
        "Adjustment support"
    };

    public static DiagnosisOption Find(string code) => All.FirstOrDefault(d => d.Code == code);
}
