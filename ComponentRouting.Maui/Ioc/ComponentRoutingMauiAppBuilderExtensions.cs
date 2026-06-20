using System;
using System.Collections.Generic;
using System.Reflection;
using ComponentRouting.Maui.Chrome;
using Microsoft.Maui.Hosting;

namespace ComponentRouting.Maui.Ioc;

public static class ComponentRoutingMauiAppBuilderExtensions
{
    public static MauiAppBuilder UseComponentRoutingMaui(
        this MauiAppBuilder builder,
        IEnumerable<Assembly>? assemblies = null,
        IEnumerable<string>? additionalManifestScopeNamePrefixes = null,
        Action<ComponentChromeConfiguration>? configureChrome = null)
    {
        builder.Services.RegisterComponentRoutingMauiServices(
            assemblies,
            additionalManifestScopeNamePrefixes,
            configureChrome);
        builder.Services.RegisterComponentRoutingMauiPlatformChromeServices();

#if IOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(
                typeof(ComponentRoutingStatusBarNavigationPage),
                typeof(ComponentRoutingStatusBarNavigationRenderer));
        });
#endif
        return builder;
    }
}
