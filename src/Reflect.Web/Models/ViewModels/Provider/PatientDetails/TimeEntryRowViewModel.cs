using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class TimeEntryRowViewModel
{
    public string ServiceDate { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public bool IsInteractive { get; set; }
    public string Method { get; set; } = string.Empty;
    public string MethodIcon { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public static TimeEntryRowViewModel FromEntity(ProviderTimeEntry entry) => new()
    {
        ServiceDate = entry.ServiceDate.ToUsDateString(),
        Activity = ProviderUi.ActivityLabel(entry.Activity),
        Minutes = entry.Minutes,
        IsInteractive = entry.IsInteractiveCommunication,
        Method = ProviderUi.MethodLabel(entry.CommunicationMethod),
        MethodIcon = ProviderUi.MethodIcon(entry.CommunicationMethod),
        Notes = entry.Notes
    };
}
