using ComponentRouting.Maui.Model.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHost
{
    private readonly AbsoluteLayout containerLayout;

    public OverlaySurfaceHost(Component parentComponent, AbsoluteLayout containerLayout)
    {
        ParentComponent = parentComponent;
        this.containerLayout = containerLayout;
    }

    public Component ParentComponent { get; }

    public bool Contains(Layout layout)
    {
        return containerLayout.Children.Contains(layout);
    }

    public OverlaySurfaceHandle Mount(Layout layout)
    {
        AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
        layout.IsVisible = false;
        containerLayout.Children.Add(layout);
        layout.ZIndex = 10;
        layout.IsVisible = true;

        return new OverlaySurfaceHandle(() => Unmount(layout));
    }

    private void Unmount(Layout layout)
    {
        layout.IsVisible = false;
        if (containerLayout.Children.Contains(layout))
        {
            containerLayout.Children.Remove(layout);
        }
    }
}
