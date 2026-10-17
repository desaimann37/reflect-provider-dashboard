using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class AccessLogRowViewModel
{
    public string When { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    public static AccessLogRowViewModel FromEntity(PhiAccessLog entry) => new()
    {
        When = entry.OccurredAt.ToUsDateTimeString(),
        User = entry.UserDisplayName,
        Action = entry.Action
    };
}
