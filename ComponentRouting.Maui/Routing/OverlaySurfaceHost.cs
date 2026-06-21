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
    public string HostKind => IsPlatformHost ? "platform-root" : "legacy";

    public static OverlaySurfaceHost CreateLegacy(Component parentComponent, AbsoluteLayout containerLayout)
    {
        return new OverlaySurfaceHost(
            parentComponent,
            containerLayout.Children.Contains,
            layout =>
            {
                var operationId = OverlayTraceLog.CurrentOperationId;
                OverlayTraceLog.Write(
                    $"op={operationId ?? "none"} step=host.mount.begin host=legacy parent={OverlayTraceLog.DescribeObject(parentComponent)} container={OverlayTraceLog.DescribeObject(containerLayout)} containerParent={OverlayTraceLog.DescribeObject(containerLayout.Parent)} child={OverlayTraceLog.DescribeObject(layout)} childParentBefore={OverlayTraceLog.DescribeObject(layout.Parent)} visibleBefore={layout.IsVisible} width={layout.Width} height={layout.Height} horizontal={layout.HorizontalOptions} vertical={layout.VerticalOptions}");
                AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
                layout.IsVisible = false;
                containerLayout.Children.Add(layout);
                layout.ZIndex = 10;
                layout.IsVisible = true;
                OverlayTraceLog.Write(
                    $"op={operationId ?? "none"} step=host.mount.end host=legacy child={OverlayTraceLog.DescribeObject(layout)} childParentAfter={OverlayTraceLog.DescribeObject(layout.Parent)} visibleAfter={layout.IsVisible} zIndex={layout.ZIndex} bounds={AbsoluteLayout.GetLayoutBounds(layout)} flags={AbsoluteLayout.GetLayoutFlags(layout)} childCount={containerLayout.Children.Count}");

                return new OverlaySurfaceHandle(() =>
                {
                    OverlayTraceLog.Write(
                        $"op={operationId ?? "none"} step=host.unmount.begin host=legacy child={OverlayTraceLog.DescribeObject(layout)} childParentBefore={OverlayTraceLog.DescribeObject(layout.Parent)} container={OverlayTraceLog.DescribeObject(containerLayout)} contains={containerLayout.Children.Contains(layout)}");
                    layout.IsVisible = false;
                    if (containerLayout.Children.Contains(layout))
                    {
                        containerLayout.Children.Remove(layout);
                    }
                    OverlayTraceLog.Write(
                        $"op={operationId ?? "none"} step=host.unmount.end host=legacy child={OverlayTraceLog.DescribeObject(layout)} childParentAfter={OverlayTraceLog.DescribeObject(layout.Parent)} contains={containerLayout.Children.Contains(layout)}");
                }, "legacy", operationId);
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
