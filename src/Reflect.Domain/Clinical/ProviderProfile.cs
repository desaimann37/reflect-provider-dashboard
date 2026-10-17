using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>A clinician who monitors patients. <see cref="UserId"/> is the real app's local user id.</summary>
public class ProviderProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Credentials { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(Credentials)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {LastName}, {Credentials}";

    public string Initials => $"{FirstName[..1]}{LastName[..1]}".ToUpperInvariant();
}
