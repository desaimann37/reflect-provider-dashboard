using Reflect.Domain.Analytics;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;

namespace Reflect.Domain.Tests;

public class EngagementEventMapperTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 10, 22, 0, 0, TimeSpan.Zero);

    private static AnalyticsEvent Evt(string user, string name, DateTimeOffset at, string app, string session,
        string module = "", string moduleName = "", string duration = "")
    {
        var p = new Dictionary<string, string> { [AnalyticsEventNames.ParamSessionId] = session };
        if (module.Length > 0) p[AnalyticsEventNames.ParamModuleId] = module;
        if (moduleName.Length > 0) p[AnalyticsEventNames.ParamModuleName] = moduleName;
        if (duration.Length > 0) p[AnalyticsEventNames.ParamDurationSeconds] = duration;
        return new AnalyticsEvent(user, name, at.ToUnixTimeMilliseconds() * 1000, app, p);
    }

    [Fact]
    public void Builds_one_session_per_user_and_session_id()
    {
        var events = new[]
        {
            Evt("u1", AnalyticsEventNames.ModuleStart, T0, AnalyticsEventNames.ReflectXrAppId, "s1", "xr.creative_calm", "Creative Calm"),
            Evt("u1", AnalyticsEventNames.CreationSaved, T0.AddMinutes(10), AnalyticsEventNames.ReflectXrAppId, "s1"),
            Evt("u1", AnalyticsEventNames.CreationSaved, T0.AddMinutes(15), AnalyticsEventNames.ReflectXrAppId, "s1"),
            Evt("u1", AnalyticsEventNames.ModuleComplete, T0.AddMinutes(25), AnalyticsEventNames.ReflectXrAppId, "s1", duration: "1440"),
            Evt("u2", AnalyticsEventNames.ModuleStart, T0.AddHours(1), AnalyticsEventNames.ReflectMobileAppId, "s9", "mobile.reflect", "Reflect"),
            Evt("u2", AnalyticsEventNames.CreationSaved, T0.AddHours(1).AddMinutes(6), AnalyticsEventNames.ReflectMobileAppId, "s9"),
        };

        var sessions = EngagementEventMapper.Map(events);

        Assert.Equal(2, sessions.Count);
        var vr = sessions[0];
        Assert.Equal("u1", vr.AppUserKey);
        Assert.Equal(ReflectPlatform.VirtualReality, vr.Platform);
        Assert.Equal("Creative Calm", vr.ModuleName);
        Assert.Equal(1440, vr.DurationSeconds);
        Assert.True(vr.Completed);
        Assert.Equal(2, vr.CreationsSaved);
        Assert.Equal(T0, vr.StartedAt);

        var mobile = sessions[1];
        Assert.Equal(ReflectPlatform.Mobile, mobile.Platform);
        Assert.False(mobile.Completed);
        Assert.Equal(360, mobile.DurationSeconds);
    }

    [Fact]
    public void Events_without_a_session_id_are_ignored()
    {
        var orphan = new AnalyticsEvent("u1", "screen_view", T0.ToUnixTimeMilliseconds() * 1000, AnalyticsEventNames.ReflectMobileAppId,
            new Dictionary<string, string>());

        Assert.Empty(EngagementEventMapper.Map(new[] { orphan }));
    }
}
