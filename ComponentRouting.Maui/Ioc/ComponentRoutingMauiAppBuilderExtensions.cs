using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using ComponentRouting.Maui.Chrome;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
#if ANDROID
using Microsoft.Maui.LifecycleEvents;
#endif

namespace ComponentRouting.Maui.Ioc;

public static class ComponentRoutingMauiAppBuilderExtensions
{
    private static readonly ConditionalWeakTable<MauiAppBuilder, object> BuildersWithAutomaticPlatformLifecycle = new();
    private static readonly object AutomaticPlatformLifecycleSync = new();

    public static MauiAppBuilder UseComponentRoutingMaui(
        this MauiAppBuilder builder,
        IEnumerable<Assembly>? assemblies = null,
        IEnumerable<string>? additionalManifestScopeNamePrefixes = null,
        Action<ComponentChromeConfiguration>? configureChrome = null,
        Action<ComponentRoutingMauiRuntimeOptions>? configureRuntime = null)
    {
        var runtimeOptions = new ComponentRoutingMauiRuntimeOptions();
        configureRuntime?.Invoke(runtimeOptions);

        builder.Services.RegisterComponentRoutingMauiServices(
            assemblies,
            additionalManifestScopeNamePrefixes,
            configureChrome);
        builder.Services.RegisterComponentRoutingMauiPlatformChromeServices();

        if (runtimeOptions.UseAutomaticPlatformLifecycle)
        {
            ComponentRoutingMauiLifecycleDiagnostics.MarkAutomaticPlatformLifecycleEnabled();
            RegisterAutomaticPlatformLifecycle(builder, runtimeOptions);
        }

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

    private static void RegisterAutomaticPlatformLifecycle(
        MauiAppBuilder builder,
        ComponentRoutingMauiRuntimeOptions runtimeOptions)
    {
        if (!TryMarkAutomaticPlatformLifecycleRegistered(builder))
            return;

#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddAndroid(android => android
                .OnCreate((_, _) =>
                {
                    IPlatformApplication.Current?.Services.GetService<Router>()?.BeginNewRuntime();
                })
                .OnDestroy(activity =>
                {
                    var router = IPlatformApplication.Current?.Services.GetService<Router>();
                    if (router is null)
                        return;

                    _ = router.ShutdownAsync(runtimeOptions.GetAndroidOnDestroyShutdownOptions());
                }));
        });
#endif
    }

    private static bool TryMarkAutomaticPlatformLifecycleRegistered(MauiAppBuilder builder)
    {
        lock (AutomaticPlatformLifecycleSync)
        {
            if (BuildersWithAutomaticPlatformLifecycle.TryGetValue(builder, out _))
                return false;

            BuildersWithAutomaticPlatformLifecycle.Add(builder, new object());
            return true;
        }
    }
}
