using Reflect.Domain.Common;
using Reflect.Domain.Interfaces;
using Reflect.Web.Standins.Auth;
using Reflect.Web.Standins.Data;

namespace Reflect.Web.Standins;

/// <summary>STAND-IN registrations. On merge the real app's auth, user service and EF Core repositories replace all of these.</summary>
public static class StandinServiceCollectionExtensions
{
    public static IServiceCollection AddDemoStandins(this IServiceCollection services)
    {
        services.AddAuthentication(DemoAuthenticationHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DemoAuthenticationHandler>(DemoAuthenticationHandler.SchemeName, _ => { });

        services.AddScoped<ICurrentUserService, DemoCurrentUserService>();

        services.AddSingleton<InMemoryStore>();
        services.AddSingleton<IProviderProfileRepository, InMemoryProviderProfileRepository>();
        services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
        services.AddSingleton<IPatientAppLinkRepository, InMemoryPatientAppLinkRepository>();
        services.AddSingleton<ICarePlanRepository, InMemoryCarePlanRepository>();
        services.AddSingleton<IObservationRepository, InMemoryObservationRepository>();
        services.AddSingleton<IProviderTimeEntryRepository, InMemoryProviderTimeEntryRepository>();
        services.AddSingleton<IClinicalNoteRepository, InMemoryClinicalNoteRepository>();
        services.AddSingleton<IRtmPeriodReviewRepository, InMemoryRtmPeriodReviewRepository>();
        services.AddSingleton<IPhiAccessLogRepository, InMemoryPhiAccessLogRepository>();
        services.AddSingleton<IEngagementSessionRepository, InMemoryEngagementSessionRepository>();
        services.AddSingleton<IAnalyticsEventSource, SyntheticAnalyticsEventSource>();
        return services;
    }

    /// <summary>Seeds synthetic data and builds the session read model from the synthetic analytics events.</summary>
    public static async Task SeedDemoDataAsync(this IServiceProvider services)
    {
        var store = services.GetRequiredService<InMemoryStore>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        SyntheticDataSeeder.Seed(store, now);
        await EngagementSync.RunAsync(services.GetRequiredService<IAnalyticsEventSource>(), store, now.AddDays(-400), now, CancellationToken.None);
        SyntheticDataSeeder.SeedSignedReviews(store, now);
    }
}
