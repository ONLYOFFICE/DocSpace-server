using Microsoft.AspNetCore.Mvc;

namespace PluginLibrary;

public class PluginRouteAttribute : RouteAttribute
{
    private static string BaseUrl { get; }

    static PluginRouteAttribute()
    {
        BaseUrl = "plugin/[controller]";
    }

    public PluginRouteAttribute() : base(BaseUrl) { }
    public PluginRouteAttribute(string template) : base($"{BaseUrl}/{template}") { }
}
