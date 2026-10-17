using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.Messages;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.SignOff;

/// <summary>A single-click confirmed action: the provider attests the current period's documentation.</summary>
public class RtmSignOffViewModel
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public string PeriodStart { get; set; } = string.Empty;

    public async Task<RtmSignOffSaveData> PrepareDataForSave(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, RtmRules rules, ModelStateDictionary modelState, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var summary = await CaseloadDataLoader.LoadPatientAsync(repos, provider, PatientId, now, rules, ct);
        var rtm = summary.Rtm;

        if (!DateOnly.TryParseExact(PeriodStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodStart) || periodStart != rtm.Period.Start)
        {
            modelState.AddModelError(string.Empty, "Only the current monitoring period can be signed here.");
            return null;
        }
        if (rtm.Readiness == RtmReadiness.Signed)
        {
            modelState.AddModelError(string.Empty, ProviderMessages.PeriodAlreadySigned);
            return null;
        }
        if (rtm.DataDays < rules.ShortSupplyMinDays)
        {
            modelState.AddModelError(string.Empty, ProviderMessages.SignOffNeedsData(rules.ShortSupplyMinDays));
            return null;
        }

        var existing = summary.Reviews.FirstOrDefault(r => r.PeriodStart == rtm.Period.Start);
        var review = existing ?? new RtmPeriodReview { PatientId = summary.Patient.Id, PeriodStart = rtm.Period.Start, DateCreated = now };
        review.PeriodEnd = rtm.Period.End;
        review.Status = DocumentationStatus.Signed;
        review.DataDaysAtSignOff = rtm.DataDays;
        review.ManagementMinutesAtSignOff = rtm.ManagementMinutes;
        review.CodesAttested = string.Join(", ", rtm.EligibleCodes.Select(c => c.Label));
        review.SignedByProviderId = provider.Id;
        review.SignedByName = provider.DisplayName;
        review.SignedAt = now;

        return new RtmSignOffSaveData
        {
            Review = review,
            IsNew = existing == null,
            PatientName = summary.Patient.FullName,
            PeriodLabel = ProviderUi.PeriodLabel(rtm.Period)
        };
    }
}

public sealed class RtmSignOffSaveData
{
    public RtmPeriodReview Review { get; init; }
    public bool IsNew { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PeriodLabel { get; init; } = string.Empty;
}
