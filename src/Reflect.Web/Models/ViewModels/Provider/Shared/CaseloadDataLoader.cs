using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>Loads everything for a set of patients in a fixed number of queries and builds their summaries.</summary>
public static class CaseloadDataLoader
{
    public const int HistoryDays = 200;

    public static async Task<ProviderProfile> GetCurrentProviderAsync(ProviderRepositories repos, ICurrentUserService currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
        {
            throw new EntityNotFoundException("No signed-in user.");
        }
        return await repos.Providers.GetByUserIdAsync(userId, ct)
            ?? throw new EntityNotFoundException("No provider profile for the signed-in user.");
    }

    public static async Task<IReadOnlyList<PatientMonitoringSummary>> LoadCaseloadAsync(
        ProviderRepositories repos, ProviderProfile provider, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var patients = await repos.Patients.GetByProviderAsync(provider.Id, ct);
        return await LoadAsync(repos, patients, now, rules, ct);
    }

    public static async Task<PatientMonitoringSummary> LoadPatientAsync(
        ProviderRepositories repos, ProviderProfile provider, Guid patientId, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var patient = await repos.Patients.GetForProviderAsync(patientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {patientId} was not found.");
        var summaries = await LoadAsync(repos, new[] { patient }, now, rules, ct);
        return summaries.FirstOrDefault() ?? throw new EntityNotFoundException($"Patient {patientId} has no care plan.");
    }

    public static async Task<IReadOnlyList<PatientMonitoringSummary>> LoadAsync(
        ProviderRepositories repos, IReadOnlyList<Patient> patients, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        if (patients.Count == 0)
        {
            return Array.Empty<PatientMonitoringSummary>();
        }

        var ids = patients.Select(p => p.Id).ToList();
        var from = now.AddDays(-HistoryDays);
        var today = now.ToEasternDate();

        var plans = await repos.CarePlans.GetByPatientIdsAsync(ids, ct);
        var links = await repos.AppLinks.GetByPatientIdsAsync(ids, ct);
        var sessions = await repos.Sessions.GetByAppUserKeysAsync(links.Select(l => l.AppUserKey).ToList(), from, now, ct);
        var observations = await repos.Observations.GetByPatientIdsAsync(ids, from, now, ct);
        var timeEntries = await repos.TimeEntries.GetByPatientIdsAsync(ids, today.AddDays(-HistoryDays), today, ct);
        var reviews = await repos.Reviews.GetByPatientIdsAsync(ids, ct);

        var sessionsByKey = sessions.ToLookup(s => s.AppUserKey);
        var observationsByPatient = observations.ToLookup(o => o.PatientId);
        var timeByPatient = timeEntries.ToLookup(t => t.PatientId);
        var reviewsByPatient = reviews.ToLookup(r => r.PatientId);

        var summaries = new List<PatientMonitoringSummary>(patients.Count);
        foreach (var patient in patients)
        {
            var plan = plans.FirstOrDefault(p => p.PatientId == patient.Id);
            if (plan == null)
            {
                continue;
            }
            var link = links.FirstOrDefault(l => l.PatientId == patient.Id);
            summaries.Add(PatientMonitoringSummaryBuilder.Build(
                patient,
                plan,
                link,
                link == null ? Array.Empty<Reflect.Domain.Analytics.EngagementSession>() : sessionsByKey[link.AppUserKey].ToList(),
                observationsByPatient[patient.Id].ToList(),
                timeByPatient[patient.Id].ToList(),
                reviewsByPatient[patient.Id].ToList(),
                now,
                rules));
        }
        return summaries;
    }
}
