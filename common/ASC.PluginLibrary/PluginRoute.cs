using Microsoft.AspNetCore.Mvc;

namespace PluginLibrary;

public class PluginRouteAttribute : RouteAttribute
{
    private static string BaseUrl { get; }

    static PluginRouteAttribute()
    {
        BaseUrl = "plugin/{0}/[controller]";
    }
    public PluginRouteAttribute(string asseblyName) : base(string.Format(BaseUrl, asseblyName)) { }
}
