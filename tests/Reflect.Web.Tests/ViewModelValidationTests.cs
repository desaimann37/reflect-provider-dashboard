using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.LogTime;
using Reflect.Web.Models.ViewModels.Provider.Shared;
using Reflect.Web.Models.ViewModels.Provider.SignOff;
using Reflect.Web.Standins.Auth;
using Reflect.Web.Standins.Data;

namespace Reflect.Web.Tests;

/// <summary>ViewModel save paths against the synthetic data: validation, caseload scoping, and sign-off rules.</summary>
public class ViewModelValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private sealed class FakeUser : ICurrentUserService
    {
        public FakeUser(DemoUser user) => UserId = user.UserId;
        public Guid? UserId { get; }
        public string EntraObjectId => string.Empty;
        public string Email => string.Empty;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == ProviderRoles.Provider;
    }

    private static (ProviderRepositories Repos, InMemoryStore Store) Seeded()
    {
        var store = new InMemoryStore();
        SyntheticDataSeeder.Seed(store, Now);
        EngagementSync.RunAsync(new SyntheticAnalyticsEventSource(store), store, Now.AddDays(-400), Now, CancellationToken.None).GetAwaiter().GetResult();
        SyntheticDataSeeder.SeedSignedReviews(store, Now);
        var repos = new ProviderRepositories(
            new InMemoryProviderProfileRepository(store), new InMemoryPatientRepository(store), new InMemoryPatientAppLinkRepository(store),
            new InMemoryCarePlanRepository(store), new InMemoryObservationRepository(store), new InMemoryProviderTimeEntryRepository(store),
            new InMemoryClinicalNoteRepository(store), new InMemoryRtmPeriodReviewRepository(store), new InMemoryPhiAccessLogRepository(store),
            new InMemoryEngagementSessionRepository(store));
        return (repos, store);
    }

    private static Patient PatientOf(InMemoryStore store, DemoUser user, string firstName)
    {
        var provider = store.Providers.First(p => p.UserId == user.UserId);
        return store.Patients.First(p => p.PrimaryProviderId == provider.Id && p.FirstName == firstName);
    }

    [Fact]
    public async Task Log_time_rejects_future_dates_and_missing_call_method()
    {
        var (repos, store) = Seeded();
        var vm = new LogTimeViewModel
        {
            PatientId = PatientOf(store, DemoUsers.Okafor, "Naomi").Id,
            ServiceDate = "12/31/2030",
            Minutes = 15,
            Activity = TimeActivity.InteractiveCommunication,
            IsInteractiveCommunication = true,
            CommunicationMethod = CommunicationMethod.None
        };
        var modelState = new ModelStateDictionary();

        var result = await vm.PrepareDataForSave(repos, new FakeUser(DemoUsers.Okafor), Now, modelState, CancellationToken.None);

        Assert.Null(result);
        Assert.True(modelState.ContainsKey(nameof(LogTimeViewModel.ServiceDate)));
        Assert.True(modelState.ContainsKey(nameof(LogTimeViewModel.CommunicationMethod)));
    }

    [Fact]
    public async Task Log_time_builds_an_entry_without_saving_it()
    {
        var (repos, store) = Seeded();
        var patient = PatientOf(store, DemoUsers.Okafor, "Naomi");
        var before = store.TimeEntries.Count;
        var vm = new LogTimeViewModel
        {
            PatientId = patient.Id,
            ServiceDate = "10/15/2026",
            Minutes = 12,
            Activity = TimeActivity.InteractiveCommunication,
            IsInteractiveCommunication = true,
            CommunicationMethod = CommunicationMethod.Phone,
            Notes = "  Discussed sleep routine.  "
        };

        var result = await vm.PrepareDataForSave(repos, new FakeUser(DemoUsers.Okafor), Now, new ModelStateDictionary(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 10, 15), result.Entry.ServiceDate);
        Assert.Equal("Discussed sleep routine.", result.Entry.Notes);
        Assert.Equal(before, store.TimeEntries.Count);
    }

    [Fact]
    public async Task Another_providers_patient_is_not_found()
    {
        var (repos, store) = Seeded();
        var vm = new LogTimeViewModel { PatientId = PatientOf(store, DemoUsers.Okafor, "Naomi").Id, ServiceDate = "10/15/2026", Minutes = 5, Activity = TimeActivity.DataReview };

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            vm.PrepareDataForSave(repos, new FakeUser(DemoUsers.Reyes), Now, new ModelStateDictionary(), CancellationToken.None));
    }

    [Fact]
    public async Task Sign_off_rejects_a_period_that_is_not_current()
    {
        var (repos, store) = Seeded();
        var vm = new RtmSignOffViewModel { PatientId = PatientOf(store, DemoUsers.Okafor, "Naomi").Id, PeriodStart = "2020-01-01" };
        var modelState = new ModelStateDictionary();

        var result = await vm.PrepareDataForSave(repos, new FakeUser(DemoUsers.Okafor), Now, RtmRules.Default, modelState, CancellationToken.None);

        Assert.Null(result);
        Assert.False(modelState.IsValid);
    }

    [Fact]
    public async Task Synthetic_caseload_covers_every_rtm_state()
    {
        var (repos, store) = Seeded();
        var provider = store.Providers.First(p => p.UserId == DemoUsers.Okafor.UserId);

        var caseload = await CaseloadDataLoader.LoadCaseloadAsync(repos, provider, Now, RtmRules.Default, CancellationToken.None);
        var states = caseload.Select(s => s.Rtm.Readiness).Distinct().ToList();

        Assert.Equal(16, caseload.Count);
        Assert.Contains(RtmReadiness.OnTrack, states);
        Assert.Contains(RtmReadiness.AtRisk, states);
        Assert.Contains(RtmReadiness.Inactive, states);
        Assert.Contains(caseload, s => s.Insights.Any(i => i.Kind == InsightKind.SafetyScreenPositive));
    }
}
