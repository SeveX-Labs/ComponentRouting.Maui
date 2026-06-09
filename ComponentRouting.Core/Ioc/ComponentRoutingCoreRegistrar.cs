using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ComponentRouting.Maui.Provider.Core;

namespace ComponentRouting.Maui.Ioc;

internal static class ComponentRoutingCoreRegistrar
{
    private static readonly string[] DefaultPrefixes =
    {
        "ComponentRouting.Maui",
        "ComponentRouting.Core",
        "ComponentRouting"
    };

    public static IServiceCollection AddComponentRoutingCore(
        IServiceCollection services,
        IEnumerable<Assembly>? assemblies,
        IEnumerable<string>? additionalManifestScopeNamePrefixes,
        Func<Type, ServiceLifetime?>? componentLifetimeSelector)
    {
        var discoveredTypes = GetTypes(assemblies, additionalManifestScopeNamePrefixes).ToList();

        services.TryAddSingleton<ComponentFactory, ServiceProviderComponentFactory>();

        foreach (var componentType in discoveredTypes.Where(IsConcreteComponent))
        {
            var lifetime = componentLifetimeSelector?.Invoke(componentType) ?? ServiceLifetime.Singleton;
            services.TryAdd(new ServiceDescriptor(componentType, componentType, lifetime));
        }

        RegisterFirstConcreteAs<CatalogProvider>(services, discoveredTypes, ServiceLifetime.Singleton);
        RegisterFirstConcreteAs<LocaleProvider>(services, discoveredTypes, ServiceLifetime.Singleton);
        RegisterFirstConcreteAs<Router>(services, discoveredTypes, ServiceLifetime.Singleton);

        return services;
    }

    private static IEnumerable<Type> GetTypes(
        IEnumerable<Assembly>? assemblies,
        IEnumerable<string>? additionalManifestScopeNamePrefixes)
    {
        var sourceAssemblies = assemblies?.ToArray();
        if (sourceAssemblies is null || sourceAssemblies.Length == 0)
        {
            var prefixes = DefaultPrefixes
                .Concat(additionalManifestScopeNamePrefixes ?? Enumerable.Empty<string>())
                .ToArray();

            sourceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .Where(assembly => prefixes.Any(prefix => assembly.ManifestModule.ScopeName.StartsWith(prefix)))
                .ToArray();
        }

        return sourceAssemblies
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(GetExportedTypes);
    }

    private static IEnumerable<Type> GetExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static bool IsConcreteComponent(Type type)
    {
        return typeof(Component).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false };
    }

    private static void RegisterFirstConcreteAs<TService>(
        IServiceCollection services,
        IEnumerable<Type> discoveredTypes,
        ServiceLifetime lifetime)
    {
        var implementationType = discoveredTypes.FirstOrDefault(type =>
            typeof(TService).IsAssignableFrom(type) &&
            type is { IsAbstract: false, IsInterface: false });

        if (implementationType is not null)
            services.TryAdd(new ServiceDescriptor(typeof(TService), implementationType, lifetime));
    }
}
