#if ANDROID
using System;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Graphics;
using AndroidColor = Android.Graphics.Color;
using AndroidWindow = Android.Views.Window;

namespace ComponentRouting.Maui.Chrome;

public sealed class AndroidWindowChromeApplier
{
    public void Apply(AndroidWindow window, ComponentChromeOptions options)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(options);

        var decorView = window.DecorView;
        if (decorView is null)
            return;

        ApplyDecorFitsSystemWindows(window, options);
        ApplyWindowBackground(window, decorView, options);
        ApplySystemBarBackgrounds(window, options);
        ApplySystemBarForegrounds(window, decorView, options);
        ApplyContrastEnforcement(window, options);
        ApplyDisplayCutoutMode(window, options);
    }

    private static void ApplyDecorFitsSystemWindows(AndroidWindow window, ComponentChromeOptions options)
    {
        var decorFitsSystemWindows = options.DecorFitsSystemWindows;
        if (decorFitsSystemWindows is null && options.EdgeToEdge == true)
            decorFitsSystemWindows = false;

        if (decorFitsSystemWindows is null)
            return;

        WindowCompat.SetDecorFitsSystemWindows(window, decorFitsSystemWindows.Value);
    }

    private static void ApplyWindowBackground(
        AndroidWindow window,
        Android.Views.View decorView,
        ComponentChromeOptions options)
    {
        if (options.WindowBackgroundColor is null)
            return;

        var androidColor = ToAndroidColor(options.WindowBackgroundColor);
        window.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(androidColor));
        decorView.SetBackgroundColor(androidColor);
    }

    private static void ApplySystemBarBackgrounds(AndroidWindow window, ComponentChromeOptions options)
    {
        if (options.StatusBarBackgroundColor is null && options.NavigationBarBackgroundColor is null)
            return;

#pragma warning disable CA1416, CA1422
        window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
        window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);

        if (Build.VERSION.SdkInt < BuildVersionCodes.VanillaIceCream)
        {
            if (options.StatusBarBackgroundColor is not null)
                window.SetStatusBarColor(ToAndroidColor(options.StatusBarBackgroundColor));

            if (options.NavigationBarBackgroundColor is not null)
                window.SetNavigationBarColor(ToAndroidColor(options.NavigationBarBackgroundColor));
        }
#pragma warning restore CA1416, CA1422
    }

    private static void ApplySystemBarForegrounds(
        AndroidWindow window,
        Android.Views.View decorView,
        ComponentChromeOptions options)
    {
        var statusBarForeground = options.StatusBarForeground;
        var navigationBarForeground = options.NavigationBarForeground;
        if (!ShouldApplyForeground(statusBarForeground) && !ShouldApplyForeground(navigationBarForeground))
            return;

#pragma warning disable CA1416, CA1422, CS0618
        var insetsController = WindowCompat.GetInsetsController(window, decorView);
        if (ShouldApplyForeground(statusBarForeground) && insetsController is not null)
            insetsController.AppearanceLightStatusBars = statusBarForeground == ChromeForeground.DarkContent;

        if (ShouldApplyForeground(navigationBarForeground) && insetsController is not null)
            insetsController.AppearanceLightNavigationBars = navigationBarForeground == ChromeForeground.DarkContent;

        var flags = decorView.SystemUiFlags;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && ShouldApplyForeground(statusBarForeground))
        {
            flags = statusBarForeground == ChromeForeground.DarkContent
                ? flags | SystemUiFlags.LightStatusBar
                : flags & ~SystemUiFlags.LightStatusBar;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && ShouldApplyForeground(navigationBarForeground))
        {
            flags = navigationBarForeground == ChromeForeground.DarkContent
                ? flags | SystemUiFlags.LightNavigationBar
                : flags & ~SystemUiFlags.LightNavigationBar;
        }

        decorView.SystemUiFlags = flags;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            var appearance = 0;
            var mask = 0;

            if (ShouldApplyForeground(statusBarForeground))
            {
                var statusMask = (int)WindowInsetsControllerAppearance.LightStatusBars;
                mask |= statusMask;
                if (statusBarForeground == ChromeForeground.DarkContent)
                    appearance |= statusMask;
            }

            if (ShouldApplyForeground(navigationBarForeground))
            {
                var navigationMask = (int)WindowInsetsControllerAppearance.LightNavigationBars;
                mask |= navigationMask;
                if (navigationBarForeground == ChromeForeground.DarkContent)
                    appearance |= navigationMask;
            }

            window.InsetsController?.SetSystemBarsAppearance(appearance, mask);
        }
#pragma warning restore CA1416, CA1422, CS0618
    }

    private static bool ShouldApplyForeground(ChromeForeground? foreground)
    {
        return foreground is ChromeForeground.LightContent or ChromeForeground.DarkContent;
    }

    private static void ApplyContrastEnforcement(AndroidWindow window, ComponentChromeOptions options)
    {
        if (options.StatusBarContrastEnforced is null && options.NavigationBarContrastEnforced is null)
            return;

        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            return;

#pragma warning disable CA1416, CA1422
        if (options.StatusBarContrastEnforced is not null)
            window.StatusBarContrastEnforced = options.StatusBarContrastEnforced.Value;

        if (options.NavigationBarContrastEnforced is not null)
            window.NavigationBarContrastEnforced = options.NavigationBarContrastEnforced.Value;
#pragma warning restore CA1416, CA1422
    }

    private static void ApplyDisplayCutoutMode(AndroidWindow window, ComponentChromeOptions options)
    {
        if (options.DisplayCutoutMode is null or ComponentDisplayCutoutMode.Default)
            return;

        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            return;

        var attributes = window.Attributes;
        if (attributes is null)
            return;

#pragma warning disable CA1416
        attributes.LayoutInDisplayCutoutMode = options.DisplayCutoutMode switch
        {
            ComponentDisplayCutoutMode.Never => LayoutInDisplayCutoutMode.Never,
            ComponentDisplayCutoutMode.ShortEdges => LayoutInDisplayCutoutMode.ShortEdges,
            ComponentDisplayCutoutMode.Always => LayoutInDisplayCutoutMode.Always,
            _ => attributes.LayoutInDisplayCutoutMode
        };
        window.Attributes = attributes;
#pragma warning restore CA1416
    }

    private static AndroidColor ToAndroidColor(Color color)
    {
        return AndroidColor.Argb(
            (int)Math.Round(color.Alpha * 255),
            (int)Math.Round(color.Red * 255),
            (int)Math.Round(color.Green * 255),
            (int)Math.Round(color.Blue * 255));
    }
}
#endif
