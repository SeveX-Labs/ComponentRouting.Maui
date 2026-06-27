using System;
using System.Diagnostics;

namespace ComponentRouting.Maui;

internal static class ComponentRoutingMauiLifecycleDiagnostics
{
    internal const string MissingWindowLifecycleWarning =
        "ComponentRouting.Maui: automatic platform lifecycle is enabled, but no Window has been attached with UseComponentRoutingMauiLifecycle(...). " +
        "EnableAutomaticPlatformLifecycle() does not replace the MAUI Window lifecycle helper. " +
        "Attach your Window with window.UseComponentRoutingMauiLifecycle(router) to wire Window.Created and Window.Destroying to the router.";

    private static readonly object Gate = new();
    private static bool isAutomaticPlatformLifecycleEnabled;
    private static bool isWindowLifecycleAttached;
    private static bool didWriteMissingWindowLifecycleWarning;
    private static Action<string> writeWarning = WriteDebugLine;

    internal static void MarkAutomaticPlatformLifecycleEnabled()
    {
        lock (Gate)
            isAutomaticPlatformLifecycleEnabled = true;
    }

    internal static void MarkWindowLifecycleAttached()
    {
        lock (Gate)
            isWindowLifecycleAttached = true;
    }

    internal static void WarnIfAutomaticPlatformLifecycleEnabledWithoutWindowLifecycle()
    {
        Action<string> warningSink;

        lock (Gate)
        {
            if (!isAutomaticPlatformLifecycleEnabled ||
                isWindowLifecycleAttached ||
                didWriteMissingWindowLifecycleWarning)
            {
                return;
            }

            didWriteMissingWindowLifecycleWarning = true;
            warningSink = writeWarning;
        }

        try
        {
            warningSink(MissingWindowLifecycleWarning);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    internal static IDisposable UseWarningSinkForTests(Action<string> warningSink)
    {
        ArgumentNullException.ThrowIfNull(warningSink);

        lock (Gate)
        {
            var previousWarningSink = writeWarning;
            writeWarning = warningSink;
            return new DisposableAction(() =>
            {
                lock (Gate)
                    writeWarning = previousWarningSink;
            });
        }
    }

    internal static void ResetForTests()
    {
        lock (Gate)
        {
            isAutomaticPlatformLifecycleEnabled = false;
            isWindowLifecycleAttached = false;
            didWriteMissingWindowLifecycleWarning = false;
            writeWarning = WriteDebugLine;
        }
    }

    private static void WriteDebugLine(string message)
    {
        Debug.WriteLine(message);
    }

    private sealed class DisposableAction : IDisposable
    {
        private readonly Action action;
        private bool didDispose;

        public DisposableAction(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            if (didDispose)
                return;

            didDispose = true;
            action();
        }
    }
}
