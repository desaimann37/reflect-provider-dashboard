using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>Time a provider spent on RTM treatment management for one patient.</summary>
public class ProviderTimeEntry : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public DateOnly ServiceDate { get; set; }
    public int Minutes { get; set; }
    public TimeActivity Activity { get; set; }
    public bool IsInteractiveCommunication { get; set; }
    public CommunicationMethod CommunicationMethod { get; set; }
    public string Notes { get; set; } = string.Empty;
}
