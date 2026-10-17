using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Brief;

/// <summary>Prepares the de-identified input for a pre-session brief and shapes the JSON response.</summary>
public class PatientBriefViewModel
{
    public Guid PatientId { get; set; }
    public Guid ViewerUserId { get; set; }
    public string ViewerName { get; set; } = string.Empty;
    public BriefInput Input { get; set; }

    public string Source { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = new();
    public List<string> SuggestedTopics { get; set; } = new();
    public string Notice { get; set; } = string.Empty;
    public string GeneratedAt { get; set; } = string.Empty;

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var summary = await CaseloadDataLoader.LoadPatientAsync(repos, provider, patientId, now, rules, ct);
        PatientId = summary.Patient.Id;
        ViewerUserId = provider.UserId;
        ViewerName = provider.DisplayName;
        Input = BriefInputFactory.FromSummary(summary);
    }

    public void SetBrief(ClinicalBrief brief, DateTimeOffset now)
    {
        Source = brief.Source;
        ModelName = brief.ModelName;
        Headline = brief.Headline;
        Highlights = brief.Highlights.ToList();
        SuggestedTopics = brief.SuggestedTopics.ToList();
        Notice = brief.Notice;
        GeneratedAt = now.ToUsDateTimeString();
    }

    public object ToResponse() => new
    {
        source = Source,
        modelName = ModelName,
        headline = Headline,
        highlights = Highlights,
        suggestedTopics = SuggestedTopics,
        notice = Notice,
        generatedAt = GeneratedAt
    };
}
