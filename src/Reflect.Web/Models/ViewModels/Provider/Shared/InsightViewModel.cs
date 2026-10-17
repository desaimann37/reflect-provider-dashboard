using Reflect.Domain.Rules;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

public sealed class InsightViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public BadgeViewModel Severity { get; set; }
    public bool NeedsAttention { get; set; }

    public static InsightViewModel FromInsight(ClinicalInsight insight) => new()
    {
        Title = insight.Title,
        Detail = insight.Detail,
        Icon = ProviderUi.InsightIcon(insight.Kind),
        Severity = ProviderUi.Severity(insight.Severity),
        NeedsAttention = insight.NeedsAttention
    };
}
