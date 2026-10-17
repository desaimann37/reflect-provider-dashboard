using Reflect.Domain.Analytics;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

public sealed record WeekBucket(DateOnly WeekStart, int VrSessions, int MobileSessions, double VrMinutes, double MobileMinutes)
{
    public int TotalSessions => VrSessions + MobileSessions;
    public double TotalMinutes => VrMinutes + MobileMinutes;
}

public sealed record ModuleUsage(string ModuleName, ReflectPlatform Platform, int Sessions, double Minutes);

/// <summary>Engagement aggregations. Weeks start on Monday, in US Eastern time.</summary>
public static class EngagementStats
{
    public static DateOnly WeekStart(DateOnly day)
    {
        var offset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-offset);
    }

    public static IReadOnlyList<WeekBucket> Weekly(IEnumerable<EngagementSession> sessions, DateOnly today, int weeks)
    {
        var firstWeek = WeekStart(today).AddDays(-7 * (weeks - 1));
        var byWeek = sessions
            .Select(s => (Week: WeekStart(s.StartedAt.ToEasternDate()), Session: s))
            .Where(x => x.Week >= firstWeek)
            .ToLookup(x => x.Week, x => x.Session);

        var buckets = new List<WeekBucket>(weeks);
        for (var i = 0; i < weeks; i++)
        {
            var week = firstWeek.AddDays(7 * i);
            var items = byWeek[week].ToList();
            buckets.Add(new WeekBucket(
                week,
                items.Count(s => s.Platform == ReflectPlatform.VirtualReality),
                items.Count(s => s.Platform == ReflectPlatform.Mobile),
                Math.Round(items.Where(s => s.Platform == ReflectPlatform.VirtualReality).Sum(s => s.DurationMinutes), 1),
                Math.Round(items.Where(s => s.Platform == ReflectPlatform.Mobile).Sum(s => s.DurationMinutes), 1)));
        }
        return buckets;
    }

    public static IReadOnlyList<ModuleUsage> ModuleMix(IEnumerable<EngagementSession> sessions) =>
        sessions
            .GroupBy(s => (s.ModuleName, s.Platform))
            .Select(g => new ModuleUsage(g.Key.ModuleName, g.Key.Platform, g.Count(), Math.Round(g.Sum(s => s.DurationMinutes), 1)))
            .OrderByDescending(m => m.Sessions)
            .ThenBy(m => m.ModuleName, StringComparer.Ordinal)
            .ToList();

    public static IEnumerable<EngagementSession> InLastDays(IEnumerable<EngagementSession> sessions, DateOnly today, int days)
    {
        var from = today.AddDays(-(days - 1));
        return sessions.Where(s =>
        {
            var day = s.StartedAt.ToEasternDate();
            return day >= from && day <= today;
        });
    }
}
