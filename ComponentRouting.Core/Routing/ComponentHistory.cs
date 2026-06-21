using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentRouting.Maui.Routing;

internal sealed class ComponentHistory
{
    private readonly List<ComponentHistoryItem> snackbars = new();
    private readonly List<ComponentHistoryItem> popups = new();

    public IReadOnlyList<ComponentHistoryItem> Snackbars => snackbars;
    public IReadOnlyList<ComponentHistoryItem> Popups => popups;

    public ComponentHistoryItem AddSnackbar(Type parentComponentType, Component component)
    {
        return Add(snackbars, parentComponentType, component, DateTime.Now, null);
    }

    public ComponentHistoryItem AddPopup(Type parentComponentType, Component component)
    {
        return Add(popups, parentComponentType, component, DateTime.Now, null);
    }

    public ComponentHistoryItem AddSnackbar(Type parentComponentType, Component component, OverlaySurfaceHandle overlaySurfaceHandle)
    {
        return Add(snackbars, parentComponentType, component, DateTime.Now, overlaySurfaceHandle);
    }

    public ComponentHistoryItem AddPopup(Type parentComponentType, Component component, OverlaySurfaceHandle overlaySurfaceHandle)
    {
        return Add(popups, parentComponentType, component, DateTime.Now, overlaySurfaceHandle);
    }

    internal ComponentHistoryItem AddSnackbar(Type parentComponentType, Component component, DateTime timestamp)
    {
        return Add(snackbars, parentComponentType, component, timestamp, null);
    }

    internal ComponentHistoryItem AddPopup(Type parentComponentType, Component component, DateTime timestamp)
    {
        return Add(popups, parentComponentType, component, timestamp, null);
    }

    internal ComponentHistoryItem AddSnackbar(
        Type parentComponentType,
        Component component,
        DateTime timestamp,
        OverlaySurfaceHandle? overlaySurfaceHandle)
    {
        return Add(snackbars, parentComponentType, component, timestamp, overlaySurfaceHandle);
    }

    internal ComponentHistoryItem AddPopup(
        Type parentComponentType,
        Component component,
        DateTime timestamp,
        OverlaySurfaceHandle? overlaySurfaceHandle)
    {
        return Add(popups, parentComponentType, component, timestamp, overlaySurfaceHandle);
    }

    public ComponentHistoryItem? TryGetLatestSnackbar(Type parentComponentType)
    {
        return GetLatestItem(snackbars, parentComponentType);
    }

    public ComponentHistoryItem? TryGetLatestPopup(Type parentComponentType)
    {
        return GetLatestItem(popups, parentComponentType);
    }

    public ComponentHistoryItem? TryGetItem(Component component)
    {
        return popups
            .Concat(snackbars)
            .FirstOrDefault(item => ReferenceEquals(item.Component, component));
    }

    public IReadOnlyList<TComponent> GetMountedOverlayComponents<TComponent>()
        where TComponent : Component
    {
        var result = new List<TComponent>();
        var seen = new HashSet<Component>(ReferenceEqualityComparer.Instance);

        foreach (var component in popups.Concat(snackbars).Select(item => item.Component))
        {
            if (component is TComponent typedComponent && seen.Add(component))
                result.Add(typedComponent);
        }

        return result;
    }

    public TComponent? GetMountedOverlayComponent<TComponent>(bool throwIfMultiple = false)
        where TComponent : Component
    {
        if (!throwIfMultiple)
            return GetMountedOverlayComponents<TComponent>().LastOrDefault();

        var components = GetMountedOverlayComponents<TComponent>();

        if (components.Count == 0)
            return default;

        if (components.Count == 1)
            return components[0];

        throw new InvalidOperationException(
            $"Multiple mounted {typeof(TComponent).Name} instances were found. Use {nameof(Router.GetMountedOverlayComponents)}<{typeof(TComponent).Name}>() instead.");

    }

    public void CloseAllPopups()
    {
        CloseAllPopups(component => component.Unpresent());
    }

    public void CloseAllPopups(Action<Component> closePopup)
    {
        foreach (var item in popups.AsEnumerable().Reverse().ToList())
        {
            closePopup(item.Component);
            popups.Remove(item);
        }
    }

    public void DismissMostRecent(ComponentHistoryItem? snackbarItem, ComponentHistoryItem? popupItem, ComponentHistoryItem? panelItem = null)
    {
        var mostRecentHistoryItem = new[] { snackbarItem, popupItem, panelItem }
            .Where(item => item is not null)
            .OrderByDescending(item => item!.Timestamp)
            .FirstOrDefault();

        mostRecentHistoryItem?.Component.Unpresent();
    }

    public bool Remove(Component component)
    {
        var snackbarHistoryItem = snackbars.FirstOrDefault(item => ReferenceEquals(item.Component, component));
        if (snackbarHistoryItem is not null)
        {
            var removed = snackbars.Remove(snackbarHistoryItem);
            OverlayTraceLog.Write(
                $"op={snackbarHistoryItem.OverlaySurfaceHandle?.OperationId ?? OverlayTraceLog.CurrentOperationId ?? "none"} step=history.remove kind=snackbar removed={removed} component={OverlayTraceLog.DescribeObject(component)} handle={OverlayTraceLog.DescribeObject(snackbarHistoryItem.OverlaySurfaceHandle)} handleHost={snackbarHistoryItem.OverlaySurfaceHandle?.HostKind ?? "unknown"} popupCount={popups.Count} snackbarCount={snackbars.Count}");
            return removed;
        }

        var popupHistoryItem = popups.FirstOrDefault(item => ReferenceEquals(item.Component, component));
        if (popupHistoryItem is null)
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=history.remove kind=unknown removed=false component={OverlayTraceLog.DescribeObject(component)} popupCount={popups.Count} snackbarCount={snackbars.Count}");
            return false;
        }

        var popupRemoved = popups.Remove(popupHistoryItem);
        OverlayTraceLog.Write(
            $"op={popupHistoryItem.OverlaySurfaceHandle?.OperationId ?? OverlayTraceLog.CurrentOperationId ?? "none"} step=history.remove kind=popup removed={popupRemoved} component={OverlayTraceLog.DescribeObject(component)} handle={OverlayTraceLog.DescribeObject(popupHistoryItem.OverlaySurfaceHandle)} handleHost={popupHistoryItem.OverlaySurfaceHandle?.HostKind ?? "unknown"} popupCount={popups.Count} snackbarCount={snackbars.Count}");
        return popupRemoved;
    }

    public IReadOnlyList<Component> ClearSnackbars()
    {
        return Clear(snackbars);
    }

    public IReadOnlyList<Component> ClearPopups()
    {
        return Clear(popups);
    }

    public static IReadOnlyList<Component> GetResumeComponents(
        Component? mountedComponent,
        Component? currentTabComponent,
        Component? currentFlyoutComponent,
        Component? lastStackComponent)
    {
        var result = new List<Component>();
        var seen = new HashSet<Component>(ReferenceEqualityComparer.Instance);

        AddResumeComponent(mountedComponent, result, seen);
        AddResumeComponent(currentTabComponent, result, seen);
        AddResumeComponent(currentFlyoutComponent, result, seen);
        AddResumeComponent(lastStackComponent, result, seen);

        return result;
    }

    private ComponentHistoryItem Add(
        ICollection<ComponentHistoryItem> target,
        Type parentComponentType,
        Component component,
        DateTime timestamp,
        OverlaySurfaceHandle? overlaySurfaceHandle)
    {
        var item = new ComponentHistoryItem(parentComponentType, component, timestamp, overlaySurfaceHandle);
        target.Add(item);
        OverlayTraceLog.Write(
            $"op={overlaySurfaceHandle?.OperationId ?? OverlayTraceLog.CurrentOperationId ?? "none"} step=history.add kind={GetHistoryKind(target)} parentType={parentComponentType.FullName} component={OverlayTraceLog.DescribeObject(component)} handle={OverlayTraceLog.DescribeObject(overlaySurfaceHandle)} handleHost={overlaySurfaceHandle?.HostKind ?? "unknown"} count={target.Count}");
        return item;
    }

    private static ComponentHistoryItem? GetLatestItem(
        IReadOnlyList<ComponentHistoryItem> source,
        Type parentComponentType)
    {
        return source
            .Where(item => item.ParentComponentType == parentComponentType)
            .OrderByDescending(item => item.Timestamp)
            .FirstOrDefault();
    }

    private IReadOnlyList<Component> Clear(ICollection<ComponentHistoryItem> source)
    {
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=history.clear.begin kind={GetHistoryKind(source)} count={source.Count}");
        foreach (var item in source)
        {
            OverlayTraceLog.Write(
                $"op={item.OverlaySurfaceHandle?.OperationId ?? OverlayTraceLog.CurrentOperationId ?? "none"} step=history.clear.unmount kind={GetHistoryKind(source)} component={OverlayTraceLog.DescribeObject(item.Component)} handle={OverlayTraceLog.DescribeObject(item.OverlaySurfaceHandle)} handleHost={item.OverlaySurfaceHandle?.HostKind ?? "unknown"}");
            item.OverlaySurfaceHandle?.Unmount();
        }

        var components = source.Select(item => item.Component).ToList();
        source.Clear();
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=history.clear.end kind={GetHistoryKind(source)} count=0");
        return components;
    }

    private string GetHistoryKind(ICollection<ComponentHistoryItem> target)
    {
        if (ReferenceEquals(target, snackbars))
            return "snackbar";

        if (ReferenceEquals(target, popups))
            return "popup";

        return "unknown";
    }

    private static void AddResumeComponent(
        Component? component,
        ICollection<Component> result,
        ISet<Component> seen)
    {
        if (component is not null && seen.Add(component))
            result.Add(component);
    }
}

internal sealed class ComponentHistoryItem
{
    public Type ParentComponentType { get; }
    public Component Component { get; }
    public DateTime Timestamp { get; }
    public OverlaySurfaceHandle? OverlaySurfaceHandle { get; }

    public ComponentHistoryItem(
        Type parentComponentType,
        Component component,
        DateTime timestamp,
        OverlaySurfaceHandle? overlaySurfaceHandle = null)
    {
        ParentComponentType = parentComponentType;
        Component = component;
        Timestamp = timestamp;
        OverlaySurfaceHandle = overlaySurfaceHandle;
    }
}
