namespace Reflect.Domain.Rules;

public sealed record ClinicalBrief
{
    /// <summary>"ai" when written by a language model, "rules" when composed deterministically.</summary>
    public string Source { get; init; } = "rules";
    public string ModelName { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SuggestedTopics { get; init; } = Array.Empty<string>();
    public string Notice { get; init; } = string.Empty;
}
