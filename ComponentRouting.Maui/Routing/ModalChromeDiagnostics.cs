using System;
using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ComponentRouting.Maui.Abstraction.Core;

namespace ComponentRouting.Maui.Routing;

internal static class ModalChromeDiagnostics
{
    [Conditional("DEBUG")]
    public static void Log(
        string source,
        Component component,
        Page? mountablePage,
        INavigation? navigation)
    {
        try
        {
            var presenterPage = component.Presenter as Page;
            var navigationPage = component is NavigationComponent navigationComponent
                ? navigationComponent.Navigation
                : mountablePage as NavigationPage;

            WriteModalChromeDiagnostics(
                $"source=AbstractRouter.Modal.{source} " +
                $"component={component.GetType().FullName} " +
                $"presenter={component.Presenter?.GetType().FullName ?? "null"} " +
                $"presenterHash={presenterPage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"presenterHandler={presenterPage?.Handler?.GetType().FullName ?? "null"} " +
                $"presenterPlatformView={presenterPage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"presenterWindowHash={presenterPage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"mountablePage={mountablePage?.GetType().FullName ?? "null"} " +
                $"mountableHash={mountablePage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"mountableHandler={mountablePage?.Handler?.GetType().FullName ?? "null"} " +
                $"mountablePlatformView={mountablePage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"mountableWindowHash={mountablePage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageHash={navigationPage?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageRoot={navigationPage?.RootPage?.GetType().FullName ?? "null"} " +
                $"navigationPageHandler={navigationPage?.Handler?.GetType().FullName ?? "null"} " +
                $"navigationPagePlatformView={navigationPage?.Handler?.PlatformView?.GetType().FullName ?? "null"} " +
                $"navigationPageWindowHash={navigationPage?.Window?.GetHashCode().ToString("X8") ?? "null"} " +
                $"navigationPageBarBackground={MauiColorDescription(navigationPage?.BarBackgroundColor)} " +
                $"navigationPageBackground={navigationPage?.Background?.GetType().FullName ?? "null"} " +
                $"navigationNull={navigation is null} " +
                $"navigationModalStackCount={navigation?.ModalStack.Count.ToString() ?? "null"} " +
                $"navigationStackCount={navigation?.NavigationStack.Count.ToString() ?? "null"}");
        }
        catch (Exception ex)
        {
            WriteModalChromeDiagnostics($"source=AbstractRouter.Modal.{source} failed={ex}");
        }
    }

    [Conditional("DEBUG")]
    private static void WriteModalChromeDiagnostics(string message)
    {
#if ANDROID
        Android.Util.Log.Debug("ComponentRouting.ModalChrome", message);
#else
        Debug.WriteLine($"[ComponentRouting.ModalChrome] {message}");
#endif
    }

    private static string MauiColorDescription(Color? color)
    {
        if (color is null)
            return "null";

        return $"#{(int)Math.Round(color.Alpha * 255):X2}{(int)Math.Round(color.Red * 255):X2}{(int)Math.Round(color.Green * 255):X2}{(int)Math.Round(color.Blue * 255):X2}";
    }
}
