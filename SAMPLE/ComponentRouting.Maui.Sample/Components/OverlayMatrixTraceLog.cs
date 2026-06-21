using ComponentRouting.Maui.Abstraction;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ComponentRouting.Maui.Sample.Components;

internal static class OverlayMatrixTraceLog
{
    public static void Click(string context, string action, Component component)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        var navigation = page?.Navigation;
        Write(
            $"sample.click context={context} action={action} component={DescribeObject(component)} presenter={DescribeObject(component.Presenter)} page={DescribeObject(page)} navigation={DescribeObject(navigation)} navigationStackCount={navigation?.NavigationStack.Count ?? -1} modalStackCount={navigation?.ModalStack.Count ?? -1}");
    }

    [Conditional("DEBUG")]
    private static void Write(string message)
    {
#if ANDROID
        Android.Util.Log.Debug("ComponentRouting.Sample", $"[ComponentRouting][OverlayTrace] {message}");
#else
        Debug.WriteLine($"[ComponentRouting][OverlayTrace] {message}");
#endif
    }

    private static string DescribeObject(object? value)
    {
        if (value is null)
            return "null";

        var type = value.GetType();
        return $"{type.FullName}({type.Name})#{RuntimeHelpers.GetHashCode(value):X8}";
    }
}
