using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Service.Core;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
#endif

namespace ComponentRouting.Maui.Routing;

internal static class SnackbarLayoutApplier
{
    public static void ApplyDefaultLayout(Component component)
    {
        if (component is not SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter })
            return;

        snackbarPresenter.HorizontalOptions = LayoutOptions.Fill;
        snackbarPresenter.VerticalOptions = LayoutOptions.Fill;

        foreach (var child in snackbarPresenter.Children)
        {
            if (child is not View childView)
                continue;

            var flags = AbsoluteLayout.GetLayoutFlags(childView);
            var bounds = AbsoluteLayout.GetLayoutBounds(childView);

            if (flags != AbsoluteLayoutFlags.None ||
                bounds.Width != AbsoluteLayout.AutoSize ||
                bounds.Height != AbsoluteLayout.AutoSize)
            {
                continue;
            }

            AbsoluteLayout.SetLayoutFlags(
                childView,
                AbsoluteLayoutFlags.WidthProportional);
            AbsoluteLayout.SetLayoutBounds(
                childView,
                new Rect(0, 0, 1, AbsoluteLayout.AutoSize));
            childView.HorizontalOptions = LayoutOptions.Fill;
            childView.VerticalOptions = LayoutOptions.Start;
        }
    }

    public static void ApplyPlatformSafeArea(
        Component component,
        OverlaySurfaceHost surfaceHost,
        SafeAreaInsetsService safeAreaInsetsService)
    {
        if (component is not SnackbarComponent { Presenter: SnackbarPresenter snackbarPresenter })
            return;

        var topInset = GetTopInset(surfaceHost, safeAreaInsetsService);
        if (topInset <= 0)
        {
            return;
        }

        foreach (var child in snackbarPresenter.Children)
        {
            if (child is not View childView)
                continue;

            var flags = AbsoluteLayout.GetLayoutFlags(childView);
            var bounds = AbsoluteLayout.GetLayoutBounds(childView);
            if (!flags.HasFlag(AbsoluteLayoutFlags.WidthProportional) ||
                bounds.Y != 0 ||
                bounds.Height != AbsoluteLayout.AutoSize)
            {
                continue;
            }

            var margin = childView.Margin;
            childView.Margin = new Thickness(
                margin.Left,
                Math.Max(margin.Top, topInset),
                margin.Right,
                margin.Bottom);
            snackbarPresenter.InvalidateMeasure();
        }
    }

    private static double GetTopInset(
        OverlaySurfaceHost surfaceHost,
        SafeAreaInsetsService safeAreaInsetsService)
    {
        if (!surfaceHost.IsPlatformHost)
            return 0;

        var configuredTopInset = safeAreaInsetsService.GetSafeAreaInsets(true).Top;
#if ANDROID
        return Math.Max(configuredTopInset, GetAndroidStatusBarInset());
#else
        return configuredTopInset;
#endif
    }

#if ANDROID
    private static double GetAndroidStatusBarInset()
    {
        var activity = Platform.CurrentActivity;
        var density = DeviceDisplay.MainDisplayInfo.Density;
        if (density <= 0)
            density = 1;

        var resourceId = activity?.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
        if (resourceId > 0 && activity?.Resources is { } resources)
            return resources.GetDimensionPixelSize(resourceId) / density;

        return 0;
    }
#endif
}
