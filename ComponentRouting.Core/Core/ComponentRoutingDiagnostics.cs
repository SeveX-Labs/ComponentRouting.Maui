using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ComponentRouting.Maui;

// Internal, best-effort diagnostics used by broad "swallow" catches and by observed fire-and-forget
// tasks. It logs through Trace (compiled in for Release too, unlike the previous Debug.WriteLine-only
// swallows), never rethrows, and exposes a test-only sink override. This is intentionally NOT the
// process-global lifecycle diagnostics; it holds no domain state beyond the pluggable sink.
internal static class ComponentRoutingDiagnostics
{
    private static readonly object Gate = new();
    private static Action<string> sink = WriteToTrace;

    internal static void WriteException(Exception? exception, [CallerMemberName] string operationName = "")
    {
        if (exception is null)
            return;

        var message = $"[ComponentRouting] {operationName} failed: {exception}";

        Action<string> current;
        lock (Gate)
            current = sink;

        current(message);
    }

    private static void WriteToTrace(string message)
    {
        // Trace.WriteLine is active in Release (TRACE is defined by default) and still reaches the
        // debugger output in DEBUG, so best-effort failures are observable in both configurations.
        Trace.WriteLine(message);
    }

    internal static IDisposable OverrideSinkForTests(Action<string> testSink)
    {
        lock (Gate)
        {
            var previous = sink;
            sink = testSink ?? WriteToTrace;
            return new SinkScope(previous);
        }
    }

    private sealed class SinkScope : IDisposable
    {
        private readonly Action<string> previousSink;
        private bool disposed;

        internal SinkScope(Action<string> previousSink)
        {
            this.previousSink = previousSink;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            lock (Gate)
                sink = previousSink;
        }
    }
}
