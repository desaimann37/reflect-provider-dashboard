namespace Reflect.Web.Tests;

/// <summary>Locates source files relative to the repository root (the folder holding ReflectProvider.slnx).</summary>
internal static class RepoFiles
{
    private static readonly Lazy<string> RootPath = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ReflectProvider.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    });

    public static string Root => RootPath.Value;

    public static string Web => Path.Combine(Root, "src", "Reflect.Web");

    public static IEnumerable<string> Files(string relativeDir, params string[] patterns) =>
        patterns.SelectMany(p => Directory.EnumerateFiles(Path.Combine(Web, relativeDir), p, SearchOption.AllDirectories));

    /// <summary>Everything that ships with the Provider area (stand-ins excluded).</summary>
    public static IEnumerable<string> ProviderViews() => Files(Path.Combine("Areas", "Provider", "Views"), "*.cshtml");

    public static IEnumerable<string> ProviderScripts() =>
        Files(Path.Combine("wwwroot", "js", "controllers", "provider"), "*.js")
            .Concat(Files(Path.Combine("wwwroot", "js", "services", "provider"), "*.js"));

    public static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');
}
