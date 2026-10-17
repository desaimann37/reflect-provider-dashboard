using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;

namespace Reflect.Web.Infrastructure.Provider;

/// <summary>Brief generator that needs no external service. Used when no AI key is configured.</summary>
public sealed class RuleBasedClinicalBriefGenerator : IClinicalBriefGenerator
{
    public Task<ClinicalBrief> GenerateAsync(BriefInput input, CancellationToken ct) =>
        Task.FromResult(RuleBasedBriefComposer.Compose(input));
}
