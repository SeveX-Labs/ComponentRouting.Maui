using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using ComponentRouting.Maui.Abstraction;

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
        IEnumerable<string>? additionalManifestScopeNamePrefixes = null)
    {
        return ComponentRoutingCoreRegistrar.AddComponentRoutingCore(
            services,
            assemblies,
            additionalManifestScopeNamePrefixes,
            componentType => IsOverlayOrSnackbar(componentType) ? ServiceLifetime.Transient : null);
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
