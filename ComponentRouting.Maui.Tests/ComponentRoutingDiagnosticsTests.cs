using System.Collections.Concurrent;
using ComponentRouting.Maui;
using Xunit;

namespace ComponentRouting.Maui.Tests;

// Covers the internal observability helpers introduced for B9 (fire-and-forget observation) and
// B12(a) (broad-catch logging). Both live in ComponentRouting.Core and are reachable here via
// InternalsVisibleTo. The MAUI call sites (lifecycle handlers, chrome, async void Dispose) are not
// exercised here because they require a real MAUI runtime; this validates the shared primitives.
//
// The sink is process-global and xUnit runs test classes in parallel, so every assertion below
// filters on a unique operation marker instead of asserting on raw sink activity: concurrent
// WriteException calls from other test classes (several catches now route through this helper) must
// not make these tests flaky.
public class ComponentRoutingDiagnosticsTests
{
    [Fact]
    public void WriteException_routes_message_to_sink_with_operation_name()
    {
        string? captured = null;
        using (ComponentRoutingDiagnostics.OverrideSinkForTests(message =>
               {
                   if (message.Contains("DiagOp_Write"))
                       captured = message;
               }))
        {
            ComponentRoutingDiagnostics.WriteException(new InvalidOperationException("boom-write"), "DiagOp_Write");
        }

        Assert.NotNull(captured);
        Assert.Contains("DiagOp_Write", captured);
        Assert.Contains("boom-write", captured);
    }

    [Fact]
    public void WriteException_null_exception_does_not_call_sink()
    {
        var called = false;
        using (ComponentRoutingDiagnostics.OverrideSinkForTests(message =>
               {
                   if (message.Contains("DiagOp_Null"))
                       called = true;
               }))
        {
            ComponentRoutingDiagnostics.WriteException(null, "DiagOp_Null");
        }

        Assert.False(called);
    }

    [Fact]
    public async Task ForgetSafely_faulted_task_is_observed_and_logged()
    {
        var signal = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (ComponentRoutingDiagnostics.OverrideSinkForTests(message =>
               {
                   if (message.Contains("DiagOp_Faulted"))
                       signal.TrySetResult(message);
               }))
        {
            var source = new TaskCompletionSource<bool>();
            source.Task.ForgetSafely("DiagOp_Faulted");
            source.SetException(new InvalidOperationException("boom-faulted"));

            var winner = await Task.WhenAny(signal.Task, Task.Delay(2000));
            Assert.Same(signal.Task, winner);
        }

        Assert.Contains("boom-faulted", signal.Task.Result);
    }

    [Fact]
    public async Task ForgetSafely_successful_task_does_not_log()
    {
        var messages = new ConcurrentQueue<string>();
        using (ComponentRoutingDiagnostics.OverrideSinkForTests(messages.Enqueue))
        {
            var source = new TaskCompletionSource<bool>();
            source.SetResult(true);
            source.Task.ForgetSafely("DiagOp_Success");

            await Task.Delay(100);
        }

        Assert.DoesNotContain(messages, message => message.Contains("DiagOp_Success"));
    }

    [Fact]
    public void ForgetSafely_faulted_task_does_not_throw_to_caller()
    {
        var source = new TaskCompletionSource<bool>();
        source.SetException(new InvalidOperationException("boom-nothrow"));

        var thrown = Record.Exception(() => source.Task.ForgetSafely("DiagOp_NoThrow"));

        Assert.Null(thrown);

        // Observe the fault so it never escalates to an UnobservedTaskException in later tests.
        _ = source.Task.Exception;
    }
}
