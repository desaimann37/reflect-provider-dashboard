using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;

namespace Reflect.Web.Standins.Data;

/// <summary>STAND-IN for Azure SQL (clinical) and the analytics read model. Thread-safe via <see cref="Sync"/>.</summary>
public sealed class InMemoryStore
{
    public object Sync { get; } = new();

    public List<ProviderProfile> Providers { get; } = new();
    public List<Patient> Patients { get; } = new();
    public List<PatientAppLink> AppLinks { get; } = new();
    public List<CarePlan> CarePlans { get; } = new();
    public List<Observation> Observations { get; } = new();
    public List<ProviderTimeEntry> TimeEntries { get; } = new();
    public List<ClinicalNote> Notes { get; } = new();
    public List<RtmPeriodReview> Reviews { get; } = new();
    public List<PhiAccessLog> AccessLog { get; } = new();

    /// <summary>Raw events in the Firebase / GA4 BigQuery export shape.</summary>
    public List<AnalyticsEvent> AnalyticsEvents { get; } = new();

    /// <summary>Sessions built from <see cref="AnalyticsEvents"/> by the sync step.</summary>
    public List<EngagementSession> Sessions { get; } = new();
}
