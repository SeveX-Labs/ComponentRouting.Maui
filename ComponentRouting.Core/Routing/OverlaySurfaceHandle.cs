using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceHandle
{
    private readonly Action unmount;
    private bool didUnmount;

    public OverlaySurfaceHandle(Action unmount, string? hostKind = null, string? operationId = null)
    {
        this.unmount = unmount;
        HostKind = hostKind;
        OperationId = operationId;
    }

    public string? HostKind { get; }
    public string? OperationId { get; }

    public void Unmount()
    {
        if (didUnmount)
        {
            OverlayTraceLog.Write(
                $"op={OperationId ?? "none"} step=handle.unmount.skip alreadyDisposed=true handle={OverlayTraceLog.DescribeObject(this)} hostKind={HostKind ?? "unknown"}");
            return;
        }

        didUnmount = true;
        OverlayTraceLog.Write(
            $"op={OperationId ?? "none"} step=handle.unmount.begin handle={OverlayTraceLog.DescribeObject(this)} hostKind={HostKind ?? "unknown"}");
        unmount();
        OverlayTraceLog.Write(
            $"op={OperationId ?? "none"} step=handle.unmount.end handle={OverlayTraceLog.DescribeObject(this)} hostKind={HostKind ?? "unknown"}");
    }
}
