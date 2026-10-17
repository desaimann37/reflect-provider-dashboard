using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class MeasureRowViewModel
{
    public string Date { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public string Measure { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public BadgeViewModel Band { get; set; }
    public string Change { get; set; } = string.Empty;
    public string ChangeDirection { get; set; } = string.Empty;
    public string Item9 { get; set; } = string.Empty;
    public bool Item9Positive { get; set; }

    public static IEnumerable<MeasureRowViewModel> FromSeries(IReadOnlyList<Observation> series, string code, IReadOnlyList<Observation> item9)
    {
        for (var i = 0; i < series.Count; i++)
        {
            var current = series[i];
            var max = code == ObservationCodes.Phq9Total ? 27 : 21;
            var band = code == ObservationCodes.Phq9Total ? MeasureSeverity.Phq9(current.Value) : MeasureSeverity.Gad7(current.Value);
            var change = i == 0 ? (decimal?)null : current.Value - series[i - 1].Value;
            var item = code == ObservationCodes.Phq9Total
                ? item9.FirstOrDefault(o => Math.Abs((o.EffectiveAt - current.EffectiveAt).TotalMinutes) < 30)
                : null;
            yield return new MeasureRowViewModel
            {
                Date = current.EffectiveAt.ToUsDateString(),
                SortKey = current.EffectiveAt.ToUnixTimeSeconds(),
                Measure = ObservationCodes.DisplayFor(code).Replace(" total", string.Empty),
                Score = $"{current.Value:0} / {max}",
                Band = ProviderUi.MeasureBand(band),
                Change = change switch { null => "Baseline", 0 => "No change", > 0 => $"+{change:0} (worse)", _ => $"{change:0} (better)" },
                ChangeDirection = change switch { null or 0 => "flat", > 0 => "up", _ => "down" },
                Item9 = item == null ? string.Empty : MeasureSeverity.Item9Label(item.Value),
                Item9Positive = item != null && item.Value >= 1
            };
        }
    }
}
