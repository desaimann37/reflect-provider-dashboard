using System.Globalization;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>Parses the M/d/yyyy dates the forms post, with an explicit culture.</summary>
public static class FormDates
{
    private static readonly string[] Formats = { "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };

    public static bool TryParse(string value, out DateOnly date) =>
        DateOnly.TryParseExact((value ?? string.Empty).Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
