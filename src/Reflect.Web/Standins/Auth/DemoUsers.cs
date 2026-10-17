using Reflect.Domain.Common;

namespace Reflect.Web.Standins.Auth;

public sealed record DemoUser(Guid UserId, string EntraObjectId, string Email, string DisplayName, string Description, IReadOnlyList<string> Roles);

/// <summary>STAND-IN. Fake accounts used instead of Entra sign-in. All synthetic.</summary>
public static class DemoUsers
{
    public static readonly DemoUser Okafor = new(
        Guid.Parse("3f1c6c2e-7a51-4c1b-9d0e-5a2b8c4d0001"),
        "a7d2e0f4-1b3c-4d5e-8f60-71829304a5b1",
        "maya.okafor@riverbend-health.example",
        "Dr. Maya Okafor",
        "Provider, Riverbend Behavioral Health",
        new[] { ProviderRoles.Provider, ProviderRoles.FacilityAdmin });

    public static readonly DemoUser Reyes = new(
        Guid.Parse("3f1c6c2e-7a51-4c1b-9d0e-5a2b8c4d0002"),
        "b8e3f105-2c4d-4e6f-9071-8293a415b6c2",
        "jordan.reyes@harborlight-counseling.example",
        "Jordan Reyes",
        "Provider, Harbor Light Counseling",
        new[] { ProviderRoles.Provider });

    public static readonly DemoUser Patel = new(
        Guid.Parse("3f1c6c2e-7a51-4c1b-9d0e-5a2b8c4d0003"),
        "c9f40216-3d5e-4f70-a182-93a4b526c7d3",
        "sam.patel@example.com",
        "Sam Patel",
        "End User (no Provider access)",
        new[] { ProviderRoles.EndUser });

    public static IReadOnlyList<DemoUser> All { get; } = new[] { Okafor, Reyes, Patel };

    public static DemoUser Find(string userId) =>
        All.FirstOrDefault(u => string.Equals(u.UserId.ToString(), userId, StringComparison.OrdinalIgnoreCase));
}
