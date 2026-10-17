using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>A person under a provider's care. This is PHI and lives only in the clinical store.</summary>
public class Patient : BaseEntity
{
    public Guid FacilityId { get; set; }
    public Guid PrimaryProviderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Mrn { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;

    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{FirstName[..1]}{LastName[..1]}".ToUpperInvariant();

    public int AgeOn(DateOnly day)
    {
        var age = day.Year - DateOfBirth.Year;
        if (DateOfBirth > day.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
