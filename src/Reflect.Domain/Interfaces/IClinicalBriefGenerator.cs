using Reflect.Domain.Rules;

namespace Reflect.Domain.Interfaces;

/// <summary>Writes a short pre-session brief for a provider from de-identified monitoring facts.</summary>
public interface IClinicalBriefGenerator
{
    Task<ClinicalBrief> GenerateAsync(BriefInput input, CancellationToken ct);
}
