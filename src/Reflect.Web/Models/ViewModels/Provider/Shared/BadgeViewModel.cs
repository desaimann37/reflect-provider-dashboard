namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>A status badge. Color is always paired with a text label and an icon, never color alone.</summary>
public sealed record BadgeViewModel(string Color, string Text, string Icon, string Tooltip = "");
