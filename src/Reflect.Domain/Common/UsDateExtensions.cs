using System.Globalization;

namespace Reflect.Domain.Common;

/// <summary>
/// US display helpers. Values are stored as UTC <see cref="DateTimeOffset"/> and converted to
/// US Eastern only for display. Mirrors the real app's ToUsDateString / ToUsDateTimeString helpers;
/// if the real helpers live elsewhere, delete this file on merge.
/// </summary>
public static class UsDateExtensions
{
    public const string UsDateFormat = "M/d/yyyy";
    public const string UsDateTimeFormat = "M/d/yyyy h:mm tt";

    private static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static TimeZoneInfo Eastern => EasternZone;

    public static DateTimeOffset ToEastern(this DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, EasternZone);

    /// <summary>The calendar day in US Eastern time on which an instant falls.</summary>
    public static DateOnly ToEasternDate(this DateTimeOffset value) =>
        DateOnly.FromDateTime(value.ToEastern().DateTime);

    public static string ToUsDateString(this DateTimeOffset value) =>
        value.ToEastern().ToString(UsDateFormat, CultureInfo.InvariantCulture);

    public static string ToUsDateTimeString(this DateTimeOffset value) =>
        value.ToEastern().ToString(UsDateTimeFormat, CultureInfo.InvariantCulture);

    public static string ToUsTimeString(this DateTimeOffset value) =>
        value.ToEastern().ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string ToUsDateString(this DateOnly value) =>
        value.ToString(UsDateFormat, CultureInfo.InvariantCulture);

    /// <summary>Long form used in the page header, e.g. "Friday, October 16, 2026".</summary>
    public static string ToUsLongDateString(this DateOnly value) =>
        value.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>The UTC instant at which a US Eastern calendar day starts.</summary>
    public static DateTimeOffset StartOfEasternDayUtc(this DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var offset = EasternZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
