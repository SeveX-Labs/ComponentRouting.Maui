using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHandle
{
    private readonly Action unmount;

    public OverlaySurfaceHandle(Action unmount)
    {
        this.unmount = unmount;
    }

    public void Unmount()
    {
        unmount();
    }
}
