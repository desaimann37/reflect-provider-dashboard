namespace Reflect.Domain.Analytics;

/// <summary>Custom analytics events the Reflect apps log, and the app ids that tell them apart.</summary>
public static class AnalyticsEventNames
{
    public const string ModuleStart = "module_start";
    public const string CreationSaved = "creation_saved";
    public const string ModuleComplete = "module_complete";
    public const string ModuleExit = "module_exit";

    public const string ParamSessionId = "reflect_session_id";
    public const string ParamModuleId = "module_id";
    public const string ParamModuleName = "module_name";
    public const string ParamDurationSeconds = "duration_sec";

    public const string ReflectXrAppId = "com.persistenttech.reflectxr";
    public const string ReflectMobileAppId = "com.persistenttech.reflectmobile";
}
