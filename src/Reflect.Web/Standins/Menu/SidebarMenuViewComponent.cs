using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Reflect.Web.Standins.Menu;

/// <summary>STAND-IN. Renders the Provider area's JSON menu the way the real sidebar does.</summary>
public sealed class SidebarMenuViewComponent : ViewComponent
{
    private readonly IWebHostEnvironment _env;

    public SidebarMenuViewComponent(IWebHostEnvironment env) => _env = env;

    public IViewComponentResult Invoke()
    {
        var path = Path.Combine(_env.ContentRootPath, "Areas", "Provider", "provider-menu.json");
        var items = File.Exists(path)
            ? JsonSerializer.Deserialize<List<SidebarMenuItem>>(File.ReadAllText(path)) ?? new List<SidebarMenuItem>()
            : new List<SidebarMenuItem>();
        return View(items);
    }
}
