using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHandle
{
    private readonly Action unmount;
    private bool didUnmount;

    public OverlaySurfaceHandle(Action unmount, string? hostKind = null)
    {
        this.unmount = unmount;
        HostKind = hostKind;
    }

    public string? HostKind { get; }

    public void Unmount()
    {
        if (didUnmount)
            return;

        didUnmount = true;
        unmount();
    }
}
