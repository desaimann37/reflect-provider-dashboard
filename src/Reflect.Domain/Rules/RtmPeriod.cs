namespace Reflect.Domain.Rules;

/// <summary>A 30-day RTM monitoring period (inclusive dates, US Eastern calendar days).</summary>
public readonly record struct RtmPeriod(DateOnly Start, DateOnly End)
{
    public int LengthDays => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>1-based day number of <paramref name="today"/> inside the period (clamped).</summary>
    public int DayOfPeriod(DateOnly today) =>
        Math.Clamp(today.DayNumber - Start.DayNumber + 1, 0, LengthDays);

    /// <summary>Calendar days left in the period, counting today.</summary>
    public int DaysLeftIncludingToday(DateOnly today) =>
        today > End ? 0 : End.DayNumber - Math.Max(today.DayNumber, Start.DayNumber) + 1;
}
