using Anthropic;
using Reflect.Domain.Common;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;
using Reflect.Web.Infrastructure.Provider;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider;

/// <summary>DI and authorization for the Provider area. Keep on merge.</summary>
public static class ProviderAreaRegistration
{
    public static IServiceCollection AddProviderArea(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ProviderPolicies.ProviderOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(ProviderRoles.Provider));
        });

        services.AddSingleton(RtmRules.Default);
        services.AddScoped<ProviderRepositories>();

        services.Configure<AiBriefOptions>(configuration.GetSection(AiBriefOptions.SectionName));
        var options = configuration.GetSection(AiBriefOptions.SectionName).Get<AiBriefOptions>() ?? new AiBriefOptions();
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }

        if (options.Enabled && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddSingleton(new AnthropicClient { ApiKey = apiKey });
            services.AddSingleton<IClinicalBriefGenerator, ClaudeClinicalBriefGenerator>();
        }
        else
        {
            services.AddSingleton<IClinicalBriefGenerator, RuleBasedClinicalBriefGenerator>();
        }
        return services;
    }
}
