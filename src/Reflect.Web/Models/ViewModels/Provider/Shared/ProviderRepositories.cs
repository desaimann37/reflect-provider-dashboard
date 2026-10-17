using Reflect.Domain.Interfaces;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>
/// The Provider area's repositories as one parameter. On merge these move onto the real
/// IUnitOfWork facade (unitOfWork.&lt;Entity&gt;Repo) and this class is replaced by it.
/// </summary>
public sealed class ProviderRepositories
{
    public ProviderRepositories(
        IProviderProfileRepository providers,
        IPatientRepository patients,
        IPatientAppLinkRepository appLinks,
        ICarePlanRepository carePlans,
        IObservationRepository observations,
        IProviderTimeEntryRepository timeEntries,
        IClinicalNoteRepository notes,
        IRtmPeriodReviewRepository reviews,
        IPhiAccessLogRepository accessLog,
        IEngagementSessionRepository sessions)
    {
        Providers = providers;
        Patients = patients;
        AppLinks = appLinks;
        CarePlans = carePlans;
        Observations = observations;
        TimeEntries = timeEntries;
        Notes = notes;
        Reviews = reviews;
        AccessLog = accessLog;
        Sessions = sessions;
    }

    public IProviderProfileRepository Providers { get; }
    public IPatientRepository Patients { get; }
    public IPatientAppLinkRepository AppLinks { get; }
    public ICarePlanRepository CarePlans { get; }
    public IObservationRepository Observations { get; }
    public IProviderTimeEntryRepository TimeEntries { get; }
    public IClinicalNoteRepository Notes { get; }
    public IRtmPeriodReviewRepository Reviews { get; }
    public IPhiAccessLogRepository AccessLog { get; }
    public IEngagementSessionRepository Sessions { get; }
}
