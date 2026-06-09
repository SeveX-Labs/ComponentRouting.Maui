using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Reflection;

namespace ComponentRouting.Maui.Ioc;

public static class ComponentRoutingCoreServiceCollectionExtensions
{
    public static IServiceCollection AddComponentRoutingCore(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddComponentRoutingCore((IEnumerable<Assembly>?)assemblies);
    }

    public static IServiceCollection AddComponentRoutingCore(
        this IServiceCollection services,
        IEnumerable<Assembly>? assemblies = null,
        IEnumerable<string>? additionalManifestScopeNamePrefixes = null)
    {
        return ComponentRoutingCoreRegistrar.AddComponentRoutingCore(
            services,
            assemblies,
            additionalManifestScopeNamePrefixes,
            null);
    }
}
