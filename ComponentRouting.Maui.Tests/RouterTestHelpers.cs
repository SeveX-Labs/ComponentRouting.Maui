using System.Collections;
using System.Reflection;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Routing;

namespace ComponentRouting.Maui.Tests;

internal static class RouterTestHelpers
{
    public static object AddHistoryItem(
        AbstractRouter router,
        string historyPropertyName,
        Component component,
        Type parentType,
        DateTime timestamp)
    {
        var historyType = typeof(AbstractRouter).GetNestedType("ComponentHistoryItem", BindingFlags.NonPublic)!;
        var item = Activator.CreateInstance(historyType, parentType, component)!;
        SetAutoPropertyBackingField(item, "Timestamp", timestamp);

        var list = (IList)typeof(AbstractRouter)
            .GetProperty(historyPropertyName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(router)!;

        list.Add(item);
        return item;
    }

    public static void SetRouterProperty(AbstractRouter router, string propertyName, object? value)
    {
        SetAutoPropertyBackingField(router, propertyName, value);
    }

    public static object? InvokePrivate(AbstractRouter router, string methodName, params object?[] args)
    {
        return typeof(AbstractRouter)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(router, args);
    }

    public static T GetHistoryItemComponent<T>(object historyItem)
        where T : Component
    {
        return (T)historyItem.GetType().GetProperty("Component")!.GetValue(historyItem)!;
    }

    public static List<Component> GetHistoryComponents(AbstractRouter router, string historyPropertyName)
    {
        var list = (IEnumerable)typeof(AbstractRouter)
            .GetProperty(historyPropertyName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(router)!;

        return list
            .Cast<object>()
            .Select(item => (Component)item.GetType().GetProperty("Component")!.GetValue(item)!)
            .ToList();
    }

    public static void SetComponentPresenter(Component component, Presenter presenter)
    {
        typeof(AbstractComponent<object, object>)
            .GetProperty(nameof(Component.Presenter), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(component, presenter);
    }

    private static void SetAutoPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField(
                        $"<{propertyName}>k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? typeof(AbstractRouter).GetField(
                        $"<{propertyName}>k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic);

        if (field is null)
            throw new InvalidOperationException($"Backing field for {propertyName} was not found.");

        field.SetValue(instance, value);
    }
}
