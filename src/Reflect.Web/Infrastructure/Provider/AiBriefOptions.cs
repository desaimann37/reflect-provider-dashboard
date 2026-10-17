namespace Reflect.Web.Infrastructure.Provider;

public sealed class AiBriefOptions
{
    public const string SectionName = "AiBrief";

    /// <summary>Use Claude when an API key is configured. With no key, briefs are always rule-based.</summary>
    public bool Enabled { get; set; } = true;

    public string Model { get; set; } = "claude-opus-5-5";
}
