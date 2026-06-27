using System;
using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui;

public static class ComponentRoutingMauiWindowLifecycleExtensions
{
    private static readonly BindableProperty IsLifecycleAttachedProperty =
        BindableProperty.CreateAttached(
            "IsComponentRoutingMauiLifecycleAttached",
            typeof(bool),
            typeof(ComponentRoutingMauiWindowLifecycleExtensions),
            false);

    public static Window UseComponentRoutingMauiLifecycle(
        this Window window,
        Router router,
        RouterShutdownOptions? shutdownOptions = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(router);

        ComponentRoutingMauiLifecycleDiagnostics.MarkWindowLifecycleAttached();

        if ((bool)window.GetValue(IsLifecycleAttachedProperty))
            return window;

        window.SetValue(IsLifecycleAttachedProperty, true);

        var effectiveShutdownOptions = shutdownOptions ?? CreateDefaultWindowDestroyingShutdownOptions();

        window.Created += (_, _) => router.BeginNewRuntime();
        window.Destroying += (_, _) => _ = router.ShutdownAsync(effectiveShutdownOptions);

        return window;
    }

    private static RouterShutdownOptions CreateDefaultWindowDestroyingShutdownOptions()
    {
        return new RouterShutdownOptions
        {
            Reason = RouterShutdownReason.WindowDestroying,
            DisconnectMauiPageTree = true
        };
    }
}
