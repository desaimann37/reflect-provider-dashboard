using Reflect.Domain.Clinical;

namespace Reflect.Domain.Rules;

public sealed record RtmAssessment
{
    public RtmPeriod Period { get; init; }
    public int DayOfPeriod { get; init; }
    public int DaysLeft { get; init; }
    public int DataDays { get; init; }
    public int DataDaysNeededForFullSupply { get; init; }
    public int MaxReachableDataDays { get; init; }

    /// <summary>98978, 98986, or empty when fewer than 2 data days so far.</summary>
    public string DeviceSupplyCode { get; init; } = string.Empty;

    public int ManagementMinutes { get; init; }
    public int MinutesToNextThreshold { get; init; }
    public int InteractiveCommunications { get; init; }
    public bool HasInteractiveCommunication => InteractiveCommunications > 0;
    public IReadOnlyList<BillableCode> ManagementCodes { get; init; } = Array.Empty<BillableCode>();
    public DateOnly? SetupCompletedOn { get; init; }
    public bool SetupCompletedThisPeriod { get; init; }
    public DocumentationStatus DocumentationStatus { get; init; }
    public RtmReadiness Readiness { get; init; }

    /// <summary>Plain-English reasons behind the readiness, most important first.</summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>Every code the current numbers support, in reporting order.</summary>
    public IReadOnlyList<BillableCode> EligibleCodes
    {
        get
        {
            var codes = new List<BillableCode>();
            if (SetupCompletedThisPeriod)
            {
                codes.Add(new BillableCode(RtmCodes.Setup, 1));
            }
            if (!string.IsNullOrEmpty(DeviceSupplyCode))
            {
                codes.Add(new BillableCode(DeviceSupplyCode, 1));
            }
            codes.AddRange(ManagementCodes);
            return codes;
        }
    }
}
