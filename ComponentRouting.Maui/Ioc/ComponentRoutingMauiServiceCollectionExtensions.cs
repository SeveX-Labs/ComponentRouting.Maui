using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Chrome;

namespace ComponentRouting.Maui.Ioc;

public static class ComponentRoutingMauiServiceCollectionExtensions
{
    public static IServiceCollection AddComponentRoutingMaui(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddComponentRoutingMaui((IEnumerable<Assembly>?)assemblies);
    }

    public static IServiceCollection AddComponentRoutingMaui(
        this IServiceCollection services,
        IEnumerable<Assembly>? assemblies = null,
        IEnumerable<string>? additionalManifestScopeNamePrefixes = null,
        Action<ComponentChromeConfiguration>? configureChrome = null)
    {
        var chromeConfiguration = new ComponentChromeConfiguration();
        configureChrome?.Invoke(chromeConfiguration);

        services.TryAddSingleton(chromeConfiguration);
        services.TryAddSingleton<ComponentChromeOptionsResolver>();
        services.TryAddSingleton<ComponentChromeService, NoOpComponentChromeService>();
#if ANDROID
        services.TryAddSingleton<AndroidModalWindowDiscoveryService>();
        services.TryAddSingleton<AndroidWindowChromeApplier>();
#endif

        return ComponentRoutingCoreRegistrar.AddComponentRoutingCore(
            services,
            assemblies,
            additionalManifestScopeNamePrefixes,
            componentType => IsOverlayOrSnackbar(componentType) ? ServiceLifetime.Transient : null);
    }

    public static IServiceCollection AddComponentRoutingMauiPlatformChrome(this IServiceCollection services)
    {
#if ANDROID
        services.TryAddSingleton<AndroidModalWindowDiscoveryService>();
        services.TryAddSingleton<AndroidWindowChromeApplier>();
#elif IOS
        services.TryAddSingleton<IosWindowChromeApplier>();
#endif
        services.Replace(ServiceDescriptor.Singleton<ComponentChromeService, PlatformComponentChromeService>());
        return services;
    }

    private static bool IsOverlayOrSnackbar(Type type)
    {
        return typeof(SnackbarComponent).IsAssignableFrom(type) ||
               HasGenericBaseType(type, typeof(OverlayComponent<,>));
    }

    private static bool HasGenericBaseType(Type type, Type genericTypeDefinition)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType!)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericTypeDefinition)
                return true;
        }

        return false;
    }
}
