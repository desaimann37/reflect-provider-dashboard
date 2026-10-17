namespace Reflect.Domain.Rules;

public enum RtmReadiness
{
    /// <summary>The care plan is paused or completed, so no period is being monitored.</summary>
    Inactive = 0,

    /// <summary>On pace to meet the full device-supply and management thresholds.</summary>
    OnTrack = 1,

    /// <summary>Unlikely to meet the full thresholds without action this period.</summary>
    AtRisk = 2,

    /// <summary>Thresholds met; the provider can document and sign the period.</summary>
    ReadyToSign = 3,

    /// <summary>The provider has signed the documentation for this period.</summary>
    Signed = 4
}
