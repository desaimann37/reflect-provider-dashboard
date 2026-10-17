using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Reflect.Web.Areas.Provider;
using Reflect.Web.Standins;
using Reflect.Web.Standins.Errors;
using Reflect.Web.Standins.Security;

var usCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = usCulture;
CultureInfo.DefaultThreadCurrentUICulture = usCulture;

var builder = WebApplication.CreateBuilder(args);

var mvc = builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<EntityNotFoundExceptionFilter>();
});
#pragma warning disable ASPDEPR003 // Razor runtime compilation matches the real app's configuration.
mvc.AddRazorRuntimeCompilation();
#pragma warning restore ASPDEPR003

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

// STAND-INS: fake sign-in, current user, in-memory repositories, synthetic data. Delete on merge.
builder.Services.AddDemoStandins();

// Provider area: authorization policy, RTM rules, brief generator. Keep on merge.
builder.Services.AddProviderArea(builder.Configuration);

var app = builder.Build();

await app.Services.SeedDemoDataAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(usCulture),
    SupportedCultures = new[] { usCulture },
    SupportedUICultures = new[] { usCulture }
});
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
