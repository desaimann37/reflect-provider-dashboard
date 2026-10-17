using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Domain.Tests;

internal static class TestData
{
    /// <summary>Noon US Eastern on the given day, as UTC.</summary>
    public static DateTimeOffset NoonEt(DateOnly day) => day.StartOfEasternDayUtc().AddHours(12);

    public static EngagementSession Session(DateOnly day, double minutes = 20, ReflectPlatform platform = ReflectPlatform.Mobile,
        string module = "Daily Creation", bool completed = true, string id = null) => new()
    {
        AppUserKey = "rx_test",
        SourceSessionId = id ?? Guid.NewGuid().ToString("N"),
        Platform = platform,
        ModuleName = module,
        StartedAt = NoonEt(day),
        DurationSeconds = (int)(minutes * 60),
        Completed = completed
    };

    public static Observation Obs(string code, decimal value, DateOnly day, string sessionId = "") => new()
    {
        Code = code,
        CodeSystem = ObservationCodes.SystemFor(code),
        Display = ObservationCodes.DisplayFor(code),
        Value = value,
        Source = ObservationSource.PatientReported,
        SourceSessionId = sessionId,
        EffectiveAt = NoonEt(day)
    };
}
