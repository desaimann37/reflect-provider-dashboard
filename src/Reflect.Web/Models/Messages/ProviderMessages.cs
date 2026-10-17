namespace Reflect.Web.Models.Messages;

/// <summary>User-facing message templates for the Provider area (toasts and inline notices).</summary>
public static class ProviderMessages
{
    public const string TempDataMessageKey = "ToastMessage";
    public const string TempDataTypeKey = "ToastType";

    public const string FixHighlightedFields = "Please fix the highlighted fields.";
    public const string BriefUnavailable = "Couldn't generate a brief right now. Try again in a moment.";

    public static string TimeLogged(int minutes, string patientName) =>
        $"Logged {minutes} minute{(minutes == 1 ? string.Empty : "s")} for {patientName}.";

    public static string NoteAdded(string patientName) => $"Note added to {patientName}'s record.";

    public static string CarePlanUpdated(string patientName) => $"Care plan updated for {patientName}.";

    public static string PeriodSigned(string periodLabel, string patientName) =>
        $"Signed RTM period {periodLabel} for {patientName}.";

    public static string SignOffNeedsData(int minimumDays) =>
        $"At least {minimumDays} data days are needed before a period can be signed.";

    public const string PeriodAlreadySigned = "This period has already been signed.";
}
