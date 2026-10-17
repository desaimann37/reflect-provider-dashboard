using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Interfaces;

namespace Reflect.Web.Standins.Data;

// STAND-IN in-memory repositories. Each filters soft-deleted rows, as the EF Core versions will.

public sealed class InMemoryProviderProfileRepository : IProviderProfileRepository
{
    private readonly InMemoryStore _store;
    public InMemoryProviderProfileRepository(InMemoryStore store) => _store = store;

    public Task<ProviderProfile> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        lock (_store.Sync) return Task.FromResult(_store.Providers.FirstOrDefault(p => !p.IsDeleted && p.UserId == userId));
    }

    public Task<ProviderProfile> GetByIdAsync(Guid id, CancellationToken ct)
    {
        lock (_store.Sync) return Task.FromResult(_store.Providers.FirstOrDefault(p => !p.IsDeleted && p.Id == id));
    }
}

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly InMemoryStore _store;
    public InMemoryPatientRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Patient>> GetByProviderAsync(Guid providerId, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            IReadOnlyList<Patient> result = _store.Patients
                .Where(p => !p.IsDeleted && p.PrimaryProviderId == providerId)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Patient> GetForProviderAsync(Guid patientId, Guid providerId, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Patients.FirstOrDefault(p => !p.IsDeleted && p.Id == patientId && p.PrimaryProviderId == providerId));
        }
    }
}

public sealed class InMemoryPatientAppLinkRepository : IPatientAppLinkRepository
{
    private readonly InMemoryStore _store;
    public InMemoryPatientAppLinkRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<PatientAppLink>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<PatientAppLink> result = _store.AppLinks.Where(l => !l.IsDeleted && ids.Contains(l.PatientId)).ToList();
            return Task.FromResult(result);
        }
    }
}

public sealed class InMemoryCarePlanRepository : ICarePlanRepository
{
    private readonly InMemoryStore _store;
    public InMemoryCarePlanRepository(InMemoryStore store) => _store = store;

    public Task<CarePlan> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        lock (_store.Sync) return Task.FromResult(_store.CarePlans.FirstOrDefault(c => !c.IsDeleted && c.PatientId == patientId));
    }

    public Task<IReadOnlyList<CarePlan>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<CarePlan> result = _store.CarePlans.Where(c => !c.IsDeleted && ids.Contains(c.PatientId)).ToList();
            return Task.FromResult(result);
        }
    }

    public Task UpdateAsync(CarePlan carePlan, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var index = _store.CarePlans.FindIndex(c => c.Id == carePlan.Id);
            if (index >= 0)
            {
                _store.CarePlans[index] = carePlan;
            }
        }
        return Task.CompletedTask;
    }
}

public sealed class InMemoryObservationRepository : IObservationRepository
{
    private readonly InMemoryStore _store;
    public InMemoryObservationRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Observation>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<Observation> result = _store.Observations
                .Where(o => !o.IsDeleted && ids.Contains(o.PatientId) && o.EffectiveAt >= fromUtc && o.EffectiveAt <= toUtc)
                .OrderBy(o => o.EffectiveAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(Observation observation, CancellationToken ct)
    {
        lock (_store.Sync) _store.Observations.Add(observation);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryProviderTimeEntryRepository : IProviderTimeEntryRepository
{
    private readonly InMemoryStore _store;
    public InMemoryProviderTimeEntryRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<ProviderTimeEntry>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<ProviderTimeEntry> result = _store.TimeEntries
                .Where(t => !t.IsDeleted && ids.Contains(t.PatientId) && t.ServiceDate >= from && t.ServiceDate <= to)
                .OrderByDescending(t => t.ServiceDate).ThenByDescending(t => t.DateCreated)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(ProviderTimeEntry entry, CancellationToken ct)
    {
        lock (_store.Sync) _store.TimeEntries.Add(entry);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryClinicalNoteRepository : IClinicalNoteRepository
{
    private readonly InMemoryStore _store;
    public InMemoryClinicalNoteRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<ClinicalNote>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            IReadOnlyList<ClinicalNote> result = _store.Notes
                .Where(n => !n.IsDeleted && n.PatientId == patientId)
                .OrderByDescending(n => n.AuthoredAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<ClinicalNote>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<ClinicalNote> result = _store.Notes
                .Where(n => !n.IsDeleted && ids.Contains(n.PatientId) && n.AuthoredAt >= fromUtc)
                .OrderByDescending(n => n.AuthoredAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task AddAsync(ClinicalNote note, CancellationToken ct)
    {
        lock (_store.Sync) _store.Notes.Add(note);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryRtmPeriodReviewRepository : IRtmPeriodReviewRepository
{
    private readonly InMemoryStore _store;
    public InMemoryRtmPeriodReviewRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<RtmPeriodReview>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        lock (_store.Sync)
        {
            IReadOnlyList<RtmPeriodReview> result = _store.Reviews
                .Where(r => !r.IsDeleted && ids.Contains(r.PatientId))
                .OrderByDescending(r => r.PeriodStart)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<RtmPeriodReview> GetAsync(Guid patientId, DateOnly periodStart, CancellationToken ct)
    {
        lock (_store.Sync) return Task.FromResult(_store.Reviews.FirstOrDefault(r => !r.IsDeleted && r.PatientId == patientId && r.PeriodStart == periodStart));
    }

    public Task AddAsync(RtmPeriodReview review, CancellationToken ct)
    {
        lock (_store.Sync) _store.Reviews.Add(review);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RtmPeriodReview review, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var index = _store.Reviews.FindIndex(r => r.Id == review.Id);
            if (index >= 0)
            {
                _store.Reviews[index] = review;
            }
        }
        return Task.CompletedTask;
    }
}

public sealed class InMemoryPhiAccessLogRepository : IPhiAccessLogRepository
{
    private readonly InMemoryStore _store;
    public InMemoryPhiAccessLogRepository(InMemoryStore store) => _store = store;

    public Task AddAsync(PhiAccessLog entry, CancellationToken ct)
    {
        lock (_store.Sync) _store.AccessLog.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PhiAccessLog>> GetRecentByPatientIdAsync(Guid patientId, int take, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            IReadOnlyList<PhiAccessLog> result = _store.AccessLog
                .Where(a => !a.IsDeleted && a.PatientId == patientId)
                .OrderByDescending(a => a.OccurredAt)
                .Take(take)
                .ToList();
            return Task.FromResult(result);
        }
    }
}

public sealed class InMemoryEngagementSessionRepository : IEngagementSessionRepository
{
    private readonly InMemoryStore _store;
    public InMemoryEngagementSessionRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<EngagementSession>> GetByAppUserKeysAsync(IReadOnlyCollection<string> appUserKeys, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var keys = appUserKeys.ToHashSet(StringComparer.Ordinal);
        lock (_store.Sync)
        {
            IReadOnlyList<EngagementSession> result = _store.Sessions
                .Where(s => !s.IsDeleted && keys.Contains(s.AppUserKey) && s.StartedAt >= fromUtc && s.StartedAt <= toUtc)
                .OrderBy(s => s.StartedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }
}

/// <summary>STAND-IN for the BigQuery query over the Firebase export.</summary>
public sealed class SyntheticAnalyticsEventSource : IAnalyticsEventSource
{
    private readonly InMemoryStore _store;
    public SyntheticAnalyticsEventSource(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            IReadOnlyList<AnalyticsEvent> result = _store.AnalyticsEvents
                .Where(e => e.Timestamp >= fromUtc && e.Timestamp <= toUtc)
                .ToList();
            return Task.FromResult(result);
        }
    }
}
