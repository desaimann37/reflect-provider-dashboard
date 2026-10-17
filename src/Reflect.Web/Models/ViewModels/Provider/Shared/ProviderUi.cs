using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>Maps domain states to labels, badge colors and RemixIcon v4.2 classes for the Provider area.</summary>
public static class ProviderUi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string ToJson(object value) => JsonSerializer.Serialize(value, Json);

    public static BadgeViewModel Readiness(RtmReadiness readiness) => readiness switch
    {
        RtmReadiness.OnTrack => new("info", "On track", "ri-checkbox-circle-line"),
        RtmReadiness.AtRisk => new("warning", "At risk", "ri-error-warning-line"),
        RtmReadiness.ReadyToSign => new("primary", "Ready to sign", "ri-quill-pen-line"),
        RtmReadiness.Signed => new("success", "Signed", "ri-shield-check-line"),
        _ => new("info", "Not monitoring", "ri-pause-circle-line")
    };

    public static BadgeViewModel Severity(InsightSeverity severity) => severity switch
    {
        InsightSeverity.Critical => new("danger", "Urgent", "ri-alarm-warning-line"),
        InsightSeverity.High => new("danger", "High", "ri-error-warning-line"),
        InsightSeverity.Medium => new("warning", "Review", "ri-eye-line"),
        InsightSeverity.Low => new("info", "FYI", "ri-information-line"),
        _ => new("success", "Positive", "ri-thumb-up-line")
    };

    public static string InsightIcon(InsightKind kind) => kind switch
    {
        InsightKind.SafetyScreenPositive => "ri-alarm-warning-line",
        InsightKind.Phq9Worsening or InsightKind.Gad7Worsening => "ri-arrow-up-circle-line",
        InsightKind.Phq9Improving => "ri-arrow-down-circle-line",
        InsightKind.Inactivity => "ri-zzz-line",
        InsightKind.EngagementDrop => "ri-arrow-down-line",
        InsightKind.MoodDecline => "ri-emotion-unhappy-line",
        InsightKind.MoodLift => "ri-emotion-happy-line",
        InsightKind.NoDataSinceEnrollment => "ri-cloud-off-line",
        InsightKind.RtmAtRisk => "ri-calendar-event-line",
        _ => "ri-quill-pen-line"
    };

    public static BadgeViewModel Platform(ReflectPlatform platform) => platform == ReflectPlatform.VirtualReality
        ? new("primary", "VR", "ri-goggles-line", "Reflect XR (VR headset)")
        : new("info", "Mobile", "ri-smartphone-line", "Reflect Mobile");

    public static BadgeViewModel Documentation(DocumentationStatus status) => status switch
    {
        DocumentationStatus.Signed => new("success", "Signed", "ri-shield-check-line"),
        DocumentationStatus.InProgress => new("warning", "In progress", "ri-edit-line"),
        _ => new("info", "Not started", "ri-checkbox-blank-circle-line")
    };

    public static BadgeViewModel MeasureBand(SeverityBand band) => band switch
    {
        SeverityBand.Minimal => new("success", "Minimal", "ri-heart-pulse-line"),
        SeverityBand.Mild => new("info", "Mild", "ri-heart-pulse-line"),
        SeverityBand.Moderate => new("warning", "Moderate", "ri-heart-pulse-line"),
        SeverityBand.ModeratelySevere => new("danger", "Moderately severe", "ri-heart-pulse-line"),
        _ => new("danger", "Severe", "ri-heart-pulse-line")
    };

    public static BadgeViewModel CarePlanStatus(Reflect.Domain.Clinical.CarePlanStatus status) => status switch
    {
        Reflect.Domain.Clinical.CarePlanStatus.Active => new("success", "Active", "ri-checkbox-circle-line"),
        Reflect.Domain.Clinical.CarePlanStatus.Paused => new("warning", "Paused", "ri-pause-circle-line"),
        _ => new("info", "Completed", "ri-flag-line")
    };

    public static string ActivityLabel(TimeActivity activity) => activity switch
    {
        TimeActivity.DataReview => "Data review",
        TimeActivity.InteractiveCommunication => "Interactive communication",
        TimeActivity.CarePlanUpdate => "Care plan update",
        TimeActivity.Documentation => "Documentation",
        _ => "Care coordination"
    };

    public static string MethodLabel(CommunicationMethod method) => method switch
    {
        CommunicationMethod.Phone => "Phone",
        CommunicationMethod.Video => "Video",
        CommunicationMethod.InPerson => "In person",
        _ => "None"
    };

    public static string MethodIcon(CommunicationMethod method) => method switch
    {
        CommunicationMethod.Phone => "ri-phone-line",
        CommunicationMethod.Video => "ri-video-chat-line",
        CommunicationMethod.InPerson => "ri-user-voice-line",
        _ => "ri-subtract-line"
    };

    public static string NoteTypeLabel(NoteType type) => type switch
    {
        NoteType.DataReview => "Data review",
        NoteType.SessionNote => "Session note",
        NoteType.TreatmentPlanChange => "Treatment plan change",
        _ => "Patient contact"
    };

    public static string Minutes(double minutes) =>
        string.Format(CultureInfo.InvariantCulture, "{0:0} min", minutes);

    public static string Relative(DateTimeOffset? at, DateTimeOffset now)
    {
        if (at is not DateTimeOffset value)
        {
            return "No activity";
        }
        var days = now.ToEasternDate().DayNumber - value.ToEasternDate().DayNumber;
        return days switch
        {
            <= 0 => $"Today, {value.ToUsTimeString()}",
            1 => $"Yesterday, {value.ToUsTimeString()}",
            < 7 => $"{days} days ago",
            _ => value.ToUsDateString()
        };
    }

    public static string PeriodLabel(RtmPeriod period) => $"{period.Start.ToUsDateString()} - {period.End.ToUsDateString()}";

    public static string ShortDate(DateOnly day) => day.ToString("M/d", CultureInfo.InvariantCulture);

    public static string Greeting(ProviderProfile provider) =>
        provider.Credentials is "PhD" or "PsyD" or "MD" or "DO" ? $"Dr. {provider.LastName}" : provider.FirstName;

    public static string MaskAppKey(string key) =>
        string.IsNullOrEmpty(key) ? "Not linked" : key.Length <= 7 ? key : key[..7] + "...";
}
