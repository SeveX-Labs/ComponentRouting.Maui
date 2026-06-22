using ComponentRouting.Maui.Model.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHost
{
    private readonly Func<Layout, bool> contains;
    private readonly Func<Layout, OverlaySurfaceHandle> mount;
    private readonly string hostKind;

    private OverlaySurfaceHost(
        Component parentComponent,
        string hostKind,
        Func<Layout, bool> contains,
        Func<Layout, OverlaySurfaceHandle> mount)
    {
        ParentComponent = parentComponent;
        this.hostKind = hostKind;
        this.contains = contains;
        this.mount = mount;
    }

    public Component ParentComponent { get; }
    public bool IsPlatformHost { get; private init; }
    public string HostKind => hostKind;

    public static OverlaySurfaceHost CreateLegacy(Component parentComponent, AbsoluteLayout containerLayout)
    {
        UpdateLegacyContainerInputState(containerLayout);

        return new OverlaySurfaceHost(
            parentComponent,
            "legacy",
            containerLayout.Children.Contains,
            layout =>
            {
                AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
                containerLayout.IsVisible = true;
                containerLayout.InputTransparent = false;
                layout.IsVisible = false;
                containerLayout.Children.Add(layout);
                layout.ZIndex = 10;
                layout.IsVisible = true;

                return new OverlaySurfaceHandle(() =>
                {
                    layout.IsVisible = false;
                    if (containerLayout.Children.Contains(layout))
                    {
                        containerLayout.Children.Remove(layout);
                    }
                    UpdateLegacyContainerInputState(containerLayout);
                }, "legacy");
            });
    }

    internal static void PrepareLegacyContainer(AbsoluteLayout containerLayout)
    {
        UpdateLegacyContainerInputState(containerLayout);
    }

    private static void UpdateLegacyContainerInputState(AbsoluteLayout containerLayout)
    {
        var state = LegacyOverlayContainerInputState.FromChildCount(containerLayout.Children.Count);
        containerLayout.IsVisible = state.IsVisible;
        containerLayout.InputTransparent = state.InputTransparent;
    }

    public static OverlaySurfaceHost CreatePlatform(
        Component parentComponent,
        Func<Layout, bool> contains,
        Func<Layout, OverlaySurfaceHandle> mount)
    {
        return CreatePlatform(parentComponent, "platform-root", contains, mount);
    }

    public static OverlaySurfaceHost CreatePlatform(
        Component parentComponent,
        string hostKind,
        Func<Layout, bool> contains,
        Func<Layout, OverlaySurfaceHandle> mount)
    {
        return new OverlaySurfaceHost(parentComponent, hostKind, contains, mount)
        {
            IsPlatformHost = true
        };
    }

    public bool Contains(Layout layout)
    {
        return contains(layout);
    }

    public OverlaySurfaceHandle Mount(Layout layout)
    {
        return mount(layout);
    }
}
