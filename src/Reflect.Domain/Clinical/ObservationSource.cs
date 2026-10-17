namespace Reflect.Domain.Clinical;

public enum ObservationSource
{
    /// <summary>Entered by the patient inside Reflect XR or Reflect Mobile.</summary>
    PatientReported = 1,

    /// <summary>Administered or entered by a clinician.</summary>
    ClinicianEntered = 2,

    /// <summary>Captured by a connected device (reserved for future RPM data).</summary>
    Device = 3
}
