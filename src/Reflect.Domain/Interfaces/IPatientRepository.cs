using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IPatientRepository
{
    /// <summary>Patients whose primary provider is <paramref name="providerId"/>. Never returns deleted rows.</summary>
    Task<IReadOnlyList<Patient>> GetByProviderAsync(Guid providerId, CancellationToken ct);

    /// <summary>
    /// One patient, only if it belongs to <paramref name="providerId"/>; otherwise null. Callers treat
    /// null as "not found" so a record's existence is never revealed to other providers.
    /// </summary>
    Task<Patient> GetForProviderAsync(Guid patientId, Guid providerId, CancellationToken ct);
}
