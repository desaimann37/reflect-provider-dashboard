namespace Reflect.Domain.Common;

/// <summary>
/// Role claim names. The first three exist in the real app's Entra configuration.
/// <see cref="Provider"/> is the role this extension proposes; on merge the Reflect XR team points
/// the "ProviderOnly" policy at whichever real role should see the Provider area.
/// </summary>
public static class ProviderRoles
{
    public const string EndUser = "End_User";
    public const string FacilityAdmin = "Facility_Admin";
    public const string PtiAdmin = "PTI_Admin";
    public const string Provider = "Provider";
}
