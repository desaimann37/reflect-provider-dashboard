using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Dashboard;

public class AttentionItemViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public InsightViewModel Insight { get; set; }
    public List<string> AlsoFlagged { get; set; } = new();

    /// <summary>One row per patient: the most severe flag, plus the titles of any others.</summary>
    public static AttentionItemViewModel FromInsights(PatientMonitoringSummary summary, IReadOnlyList<ClinicalInsight> flags) => new()
    {
        PatientId = summary.Patient.Id,
        PatientName = summary.Patient.FullName,
        Initials = summary.Patient.Initials,
        Insight = InsightViewModel.FromInsight(flags[0]),
        AlsoFlagged = flags.Skip(1).Select(f => f.Title).ToList()
    };
}
