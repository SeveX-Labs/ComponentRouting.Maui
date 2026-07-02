using System.Threading;
using System.Threading.Tasks;

namespace ComponentRouting.Maui;

internal static class ComponentRoutingTaskExtensions
{
    // Observes and logs the exception of a fire-and-forget task, without rethrowing to the caller,
    // without changing the task's timing, and without turning a synchronous caller into an async one.
    // Accessing the faulted task's Exception in the continuation marks it observed, so it no longer
    // surfaces as an UnobservedTaskException.
    internal static void ForgetSafely(this Task? task, string operationName)
    {
        if (task is null)
            return;

        task.ContinueWith(
            static (completed, state) =>
                ComponentRoutingDiagnostics.WriteException(completed.Exception, (string)state!),
            operationName,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
