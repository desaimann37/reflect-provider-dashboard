using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Areas.Provider;

namespace Reflect.Web.Tests;

/// <summary>
/// The tech spec's code-review rules, checked automatically. Each rule "has broken production at
/// least once", so these run on every build.
/// </summary>
public class SpecComplianceTests
{
    private static readonly Regex IconClass = new(@"\bri-[a-z0-9]+(?:-[a-z0-9]+)*\b", RegexOptions.Compiled);

    private static IEnumerable<string> ProviderSourceFiles() =>
        RepoFiles.ProviderViews()
            .Concat(RepoFiles.ProviderScripts())
            .Concat(RepoFiles.Files("Models", "*.cs"))
            .Append(Path.Combine(RepoFiles.Web, "Areas", "Provider", "provider-menu.json"));

    [Fact]
    public void Every_icon_class_exists_in_RemixIcon_4_2()
    {
        var css = File.ReadAllText(Path.Combine(RepoFiles.Web, "wwwroot", "assets", "libs", "remixicon", "fonts", "remixicon.css"));
        Assert.Contains("Remix Icon v4.2", css);
        var known = Regex.Matches(css, @"\.(ri-[a-z0-9-]+):before").Select(m => m.Groups[1].Value).ToHashSet();

        var missing = ProviderSourceFiles()
            .SelectMany(f => IconClass.Matches(File.ReadAllText(f)).Select(m => (File: RepoFiles.Relative(f), Icon: m.Value)))
            .Where(x => !known.Contains(x.Icon))
            .Select(x => $"{x.Icon} in {x.File}")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, "Icons not in RemixIcon v4.2: " + string.Join(", ", missing));
    }

    [Fact]
    public void Views_have_no_inline_event_handlers_or_javascript_links()
    {
        var offenders = RepoFiles.ProviderViews()
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\son[a-z]+\s*=\s*[""']|javascript:", RegexOptions.IgnoreCase))
            .Select(RepoFiles.Relative)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Every_inline_script_carries_the_csp_nonce()
    {
        var offenders = RepoFiles.ProviderViews()
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"<script(?![^>]*\bsrc=)[^>]*>").Select(m => (File: f, Tag: m.Value)))
            .Where(x => !x.Tag.Contains("nonce=\"@Context.GetCspNonce()\""))
            .Select(x => $"{RepoFiles.Relative(x.File)}: {x.Tag}")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Views_load_nothing_from_a_cdn()
    {
        var offenders = RepoFiles.ProviderViews()
            .Concat(RepoFiles.Files(Path.Combine("Views", "Shared"), "*.cshtml"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"(src|href)=""(https?:)?//", RegexOptions.IgnoreCase))
            .Select(RepoFiles.Relative)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Provider_stylesheet_uses_rem_and_theme_tokens()
    {
        var css = File.ReadAllText(Path.Combine(RepoFiles.Web, "wwwroot", "css", "provider", "provider.css"));
        var body = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        // Breakpoints follow Bootstrap's px grid; the rule is about text and spacing sizes.
        body = Regex.Replace(body, @"@media[^{]*", string.Empty);

        var pixelSizes = Regex.Matches(body, @"(?<![\w.])(\d*\.?\d+)px").Select(m => m.Value).Where(v => v != "1px").ToList();
        Assert.True(pixelSizes.Count == 0, "Use rem, not px: " + string.Join(", ", pixelSizes));

        var colors = Regex.Matches(body, @"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*\d").Select(m => m.Value).ToList();
        Assert.True(colors.SequenceEqual(new[] { "#1F305E" }), "Hard-coded colors (only the documented --rx-on-coral brand constant is allowed): " + string.Join(", ", colors));
    }

    [Fact]
    public void Menu_json_matches_the_real_sidebar_format()
    {
        var json = File.ReadAllText(Path.Combine(RepoFiles.Web, "Areas", "Provider", "provider-menu.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var isCategory = item.TryGetProperty("isCategory", out var c) && c.GetBoolean();
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("label").GetString()));
            if (!isCategory)
            {
                Assert.StartsWith("/Provider/", item.GetProperty("url").GetString());
                Assert.StartsWith("ri-", item.GetProperty("icon").GetString());
            }
        }
    }

    private static IEnumerable<Type> ProviderControllers() =>
        typeof(ProviderPolicies).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace == "Reflect.Web.Areas.Provider.Controllers");

    [Fact]
    public void Every_provider_controller_is_in_the_area_and_behind_the_single_policy()
    {
        var controllers = ProviderControllers().ToList();
        Assert.NotEmpty(controllers);
        foreach (var controller in controllers)
        {
            Assert.Equal("Provider", controller.GetCustomAttribute<AreaAttribute>()?.RouteValue);
            Assert.Equal(ProviderPolicies.ProviderOnly, controller.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
            Assert.True(controller.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore, $"{controller.Name} must send no-store for PHI pages.");
        }
    }

    [Fact]
    public void Every_post_action_validates_the_antiforgery_token()
    {
        var missing = ProviderControllers()
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null && m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            .Select(m => $"{m.DeclaringType.Name}.{m.Name}")
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void No_nullable_strings_anywhere_in_source()
    {
        var offenders = Directory.EnumerateFiles(Path.Combine(RepoFiles.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\bstring\?"))
            .Select(RepoFiles.Relative)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Vendored_libraries_match_the_versions_in_the_spec()
    {
        string Lib(params string[] parts) => File.ReadAllText(Path.Combine(new[] { RepoFiles.Web, "wwwroot", "assets", "libs" }.Concat(parts).ToArray()));

        Assert.Contains("jQuery v3.7.1", Lib("jquery", "jquery.min.js"));
        Assert.Contains("Bootstrap v5.3.3", Lib("bootstrap", "js", "bootstrap.bundle.min.js"));
        Assert.Contains("DataTables 1.13.8", Lib("datatables", "js", "jquery.dataTables.min.js"));
        Assert.Contains("2.5.0", Lib("datatables", "js", "dataTables.responsive.min.js"));
        Assert.Contains("4.6.13", Lib("flatpickr", "flatpickr.min.js"));
        Assert.Contains("v11.26", Lib("sweetalert2", "sweetalert2.min.js"));
        Assert.Contains("1.21.0", Lib("jquery-validation", "jquery.validate.min.js"));
    }
}
