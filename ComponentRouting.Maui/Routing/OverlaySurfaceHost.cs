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

    private OverlaySurfaceHost(
        Component parentComponent,
        Func<Layout, bool> contains,
        Func<Layout, OverlaySurfaceHandle> mount)
    {
        ParentComponent = parentComponent;
        this.contains = contains;
        this.mount = mount;
    }

    public Component ParentComponent { get; }
    public bool IsPlatformHost { get; private init; }

    public static OverlaySurfaceHost CreateLegacy(Component parentComponent, AbsoluteLayout containerLayout)
    {
        return new OverlaySurfaceHost(
            parentComponent,
            containerLayout.Children.Contains,
            layout =>
            {
                AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
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
                });
            });
    }

    public static OverlaySurfaceHost CreatePlatform(
        Component parentComponent,
        Func<Layout, bool> contains,
        Func<Layout, OverlaySurfaceHandle> mount)
    {
        return new OverlaySurfaceHost(parentComponent, contains, mount)
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
