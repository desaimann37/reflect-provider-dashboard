namespace Reflect.Domain.Clinical;

/// <summary>
/// How an interactive communication happened. RTM treatment-management codes require real-time,
/// synchronous two-way audio (video optional), so secure messages do not qualify.
/// </summary>
public enum CommunicationMethod
{
    None = 0,
    Phone = 1,
    Video = 2,
    InPerson = 3
}
