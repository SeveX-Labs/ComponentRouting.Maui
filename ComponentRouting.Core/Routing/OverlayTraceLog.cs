using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ComponentRouting.Maui.Routing;

internal static class OverlayTraceLog
{
    private static int nextOperationId;
    private static readonly AsyncLocal<string?> currentOperationId = new();

    public static string? CurrentOperationId
    {
        get => currentOperationId.Value;
        set => currentOperationId.Value = value;
    }

    public static string NewOperationId(string prefix)
    {
        return $"{prefix}-{Interlocked.Increment(ref nextOperationId):D4}";
    }

    [Conditional("DEBUG")]
    public static void Write(string message)
    {
        if (TryWriteAndroidLog(message))
            return;

        Debug.WriteLine($"[ComponentRouting][OverlayTrace] {message}");
    }

    private static bool TryWriteAndroidLog(string message)
    {
        try
        {
            var logType = Type.GetType("Android.Util.Log, Mono.Android");
            var debugMethod = logType?.GetMethod(
                "Debug",
                new[] { typeof(string), typeof(string) });
            if (debugMethod is null)
                return false;

            debugMethod.Invoke(null, new object[] { "ComponentRouting.Overlay", $"[ComponentRouting][OverlayTrace] {message}" });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string DescribeObject(object? value)
    {
        if (value is null)
            return "null";

        var type = value.GetType();
        return $"{type.FullName}({type.Name})#{RuntimeHelpers.GetHashCode(value):X8}";
    }
}
