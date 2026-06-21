using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHandle
{
    private readonly Action unmount;
    private bool didUnmount;

    public OverlaySurfaceHandle(Action unmount)
    {
        this.unmount = unmount;
    }

    public void Unmount()
    {
        if (didUnmount)
            return;

        didUnmount = true;
        unmount();
    }
}
