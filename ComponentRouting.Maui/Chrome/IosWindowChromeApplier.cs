#if IOS
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ComponentRouting.Maui.Abstraction.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using UIKit;

namespace ComponentRouting.Maui.Chrome;

public sealed class IosWindowChromeApplier
{
    public void Apply(ComponentChromeContext context)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => Apply(context));
            return;
        }

        ApplyNavigationChrome(context);
        ApplyWindowBackground(context);
    }

    private static void ApplyNavigationChrome(ComponentChromeContext context)
    {
        foreach (var navigationPage in GetNavigationPages(context))
        {
            if (context.Options.ActionBarBackgroundColor is not null)
                navigationPage.BarBackgroundColor = context.Options.ActionBarBackgroundColor;

            if (context.Options.ActionBarTextColor is not null)
                navigationPage.BarTextColor = context.Options.ActionBarTextColor;

            ApplyNativeNavigationBarAppearance(navigationPage, context.Options);
        }
    }

    private static IEnumerable<NavigationPage> GetNavigationPages(ComponentChromeContext context)
    {
        var seenPages = new HashSet<int>();

        if (context.MountablePage is NavigationPage mountableNavigationPage && TryAdd(seenPages, mountableNavigationPage))
            yield return mountableNavigationPage;

        if (context.Component.Presenter is NavigationPage presenterNavigationPage && TryAdd(seenPages, presenterNavigationPage))
            yield return presenterNavigationPage;

        if (context.Component is NavigationComponent { Navigation: not null } navigationComponent &&
            TryAdd(seenPages, navigationComponent.Navigation))
        {
            yield return navigationComponent.Navigation;
        }
    }

    private static void ApplyNativeNavigationBarAppearance(
        NavigationPage navigationPage,
        ComponentChromeOptions options)
    {
        if (options.ActionBarBackgroundColor is null && options.ActionBarTextColor is null)
            return;

        var navigationBar = GetNativeNavigationBar(navigationPage);
        if (navigationBar is null)
            return;

        var appearance = navigationBar.StandardAppearance.Copy() as UINavigationBarAppearance ?? new UINavigationBarAppearance();

        if (options.ActionBarBackgroundColor is not null)
        {
            appearance.ConfigureWithOpaqueBackground();
            appearance.BackgroundColor = options.ActionBarBackgroundColor.ToPlatform();
        }

        if (options.ActionBarTextColor is not null)
        {
            var titleTextAttributes = new UIStringAttributes
            {
                ForegroundColor = options.ActionBarTextColor.ToPlatform()
            };

            appearance.TitleTextAttributes = titleTextAttributes;
            appearance.LargeTitleTextAttributes = titleTextAttributes;
            navigationBar.TintColor = options.ActionBarTextColor.ToPlatform();
        }

        navigationBar.StandardAppearance = appearance;
        navigationBar.ScrollEdgeAppearance = appearance;
        navigationBar.CompactAppearance = appearance;
    }

    private static UINavigationBar? GetNativeNavigationBar(NavigationPage navigationPage)
    {
        return navigationPage.Handler?.PlatformView switch
        {
            UINavigationController navigationController => navigationController.NavigationBar,
            UINavigationBar navigationBar => navigationBar,
            UIViewController viewController => viewController.NavigationController?.NavigationBar,
            UIView view => FindNavigationBar(view),
            _ => null
        };
    }

    private static UINavigationBar? FindNavigationBar(UIView view)
    {
        if (view is UINavigationBar navigationBar)
            return navigationBar;

        foreach (var subview in view.Subviews)
        {
            var candidate = FindNavigationBar(subview);
            if (candidate is not null)
                return candidate;
        }

        return null;
    }

    private static void ApplyWindowBackground(ComponentChromeContext context)
    {
        // Intentionally no-op on iOS for now.
        // Even setting UIWindow.BackgroundColor can interfere with MAUI page composition on iOS 26.
        // WindowBackgroundColor remains supported by the Android platform chrome applier.
    }

    private static bool TryAdd<T>(HashSet<int> seenObjects, T value)
        where T : class
    {
        return seenObjects.Add(RuntimeHelpers.GetHashCode(value));
    }
}
#endif
