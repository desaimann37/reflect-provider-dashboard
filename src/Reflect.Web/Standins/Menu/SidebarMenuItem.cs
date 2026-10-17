using System.Text.Json.Serialization;

namespace Reflect.Web.Standins.Menu;

/// <summary>One entry in the real app's flat JSON sidebar format.</summary>
public sealed class SidebarMenuItem
{
    [JsonPropertyName("isCategory")]
    public bool IsCategory { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}
