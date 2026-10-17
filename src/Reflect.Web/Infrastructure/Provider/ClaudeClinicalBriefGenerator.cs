using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;

namespace Reflect.Web.Infrastructure.Provider;

/// <summary>
/// Writes the pre-session brief with Claude. Only the de-identified <see cref="BriefInput"/> is
/// sent (no name, MRN, dates, or patient free text). Any failure, refusal, or unparseable answer
/// falls back to the rule-based brief, so the screen always gets a result. Production use with real
/// PHI also requires a BAA with the model provider. In the real app this moves to
/// Reflect.Infrastructure next to the existing OpenAI service.
/// </summary>
public sealed class ClaudeClinicalBriefGenerator : IClinicalBriefGenerator
{
    private const string SystemPrompt =
        "You help a licensed mental-health clinician prepare for a session. You receive de-identified " +
        "monitoring data from Reflect XR (VR art-making) and Reflect Mobile: engagement counts, mood check-ins " +
        "(1-10, higher is better), PHQ-9 and GAD-7 scores with days-ago, rule-based flags, and RTM status. " +
        "Write a factual, neutral brief grounded only in the data given. Do not diagnose, do not invent facts, " +
        "and do not recommend medication. If a flag concerns safety (PHQ-9 item 9), list it first and suggest the " +
        "clinician follow their facility's safety protocol. Use plain US English. " +
        "Reply with only a JSON object: {\"headline\": string (one sentence), \"highlights\": string[] (3 to 5 short " +
        "bullets), \"suggestedTopics\": string[] (2 to 4 conversation topics for the session)}.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AnthropicClient _client;
    private readonly AiBriefOptions _options;
    private readonly ILogger<ClaudeClinicalBriefGenerator> _logger;

    public ClaudeClinicalBriefGenerator(AnthropicClient client, IOptions<AiBriefOptions> options, ILogger<ClaudeClinicalBriefGenerator> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ClinicalBrief> GenerateAsync(BriefInput input, CancellationToken ct)
    {
        var fallback = RuleBasedBriefComposer.Compose(input);
        try
        {
            var response = await _client.Beta.Messages.Create(new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = 4000,
                Betas = ["server-side-fallback-2026-07-01"],
                Fallbacks = new Default(),
                OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
                System = SystemPrompt,
                Messages =
                [
                    new() { Role = Role.User, Content = "Monitoring data (JSON):\n" + JsonSerializer.Serialize(input, JsonOptions) }
                ]
            }, ct);

            if (response.StopReason == BetaStopReason.Refusal)
            {
                _logger.LogWarning("Claude declined the brief request; using the rule-based brief.");
                return fallback with { Notice = "The AI model declined this request. Showing the rule-based summary." };
            }

            var text = string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
            var parsed = Parse(text);
            if (parsed == null)
            {
                _logger.LogWarning("Claude returned a brief that could not be parsed; using the rule-based brief.");
                return fallback;
            }

            return new ClinicalBrief
            {
                Source = "ai",
                ModelName = _options.Model,
                Headline = parsed.Headline ?? fallback.Headline,
                Highlights = parsed.Highlights?.Where(h => !string.IsNullOrWhiteSpace(h)).Take(6).ToList() ?? fallback.Highlights.ToList(),
                SuggestedTopics = parsed.SuggestedTopics?.Where(t => !string.IsNullOrWhiteSpace(t)).Take(4).ToList() ?? fallback.SuggestedTopics.ToList(),
                Notice = "AI draft written from de-identified monitoring data. Verify against the record before acting; this is not a diagnosis."
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude brief generation failed; using the rule-based brief.");
            return fallback with { Notice = "The AI brief is unavailable right now. Showing the rule-based summary." };
        }
    }

    private static BriefJson Parse(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<BriefJson>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class BriefJson
    {
        public string Headline { get; set; }
        public List<string> Highlights { get; set; }
        public List<string> SuggestedTopics { get; set; }
    }
}
