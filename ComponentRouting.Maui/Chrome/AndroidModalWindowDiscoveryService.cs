#if ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using AndroidX.Fragment.App;
using ComponentRouting.Maui.Abstraction.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using AndroidView = Android.Views.View;
using AndroidWindow = Android.Views.Window;

namespace ComponentRouting.Maui.Chrome;

public sealed class AndroidModalWindowDiscoveryService
{
    public IReadOnlyList<AndroidModalWindowCandidate> FindModalDialogWindows(
        Component component,
        Page? mountablePage = null)
    {
        return EnumerateDialogWindowCandidates(component, mountablePage)
            .Where(static candidate => candidate.MatchesModalIdentity)
            .ToArray();
    }

    public IReadOnlyList<AndroidModalWindowCandidate> EnumerateDialogWindowCandidates(
        Component component,
        Page? mountablePage = null)
    {
        ArgumentNullException.ThrowIfNull(component);

        var activity = Platform.CurrentActivity;
        var activityDecorView = activity?.Window?.DecorView;
        if (activity is not FragmentActivity fragmentActivity || activityDecorView is null)
            return Array.Empty<AndroidModalWindowCandidate>();

        var supportFragmentManager = fragmentActivity.SupportFragmentManager;
        if (supportFragmentManager.IsDestroyed)
            return Array.Empty<AndroidModalWindowCandidate>();

        var modalStackCount = GetObservedModalStackCount(component, mountablePage);
        var identity = CreateModalWindowIdentity(component, mountablePage, activityDecorView, modalStackCount);
        var candidates = new List<AndroidModalWindowCandidate>();
        CollectDialogWindowCandidates(
            supportFragmentManager,
            identity,
            activityDecorView,
            candidates,
            "support",
            0);

        return candidates;
    }

    public AndroidModalWindowIdentity CreateModalWindowIdentity(
        Component component,
        Page? mountablePage = null)
    {
        ArgumentNullException.ThrowIfNull(component);

        var activityDecorView = Platform.CurrentActivity?.Window?.DecorView;
        if (activityDecorView is null)
            return EmptyIdentity(false);

        return CreateModalWindowIdentity(
            component,
            mountablePage,
            activityDecorView,
            GetObservedModalStackCount(component, mountablePage));
    }

    private static AndroidModalWindowIdentity CreateModalWindowIdentity(
        Component component,
        Page? mountablePage,
        AndroidView activityDecorView,
        int modalStackCount)
    {
        var tokenHashes = new HashSet<int>();
        var rootHashes = new HashSet<int>();
        var viewHashes = new HashSet<int>();
        var candidates = GetComponentNativeViewCandidates(component, mountablePage).ToList();

        foreach (var candidate in candidates)
        {
            if (!IsModalTokenView(candidate.View, activityDecorView))
                continue;

            AddViewNativeIdentity(candidate.View, tokenHashes, rootHashes, viewHashes);
        }

        if (modalStackCount <= 0 || tokenHashes.Count > 0 || rootHashes.Count > 0)
            return new AndroidModalWindowIdentity(tokenHashes, rootHashes, viewHashes, false);

        foreach (var candidate in candidates)
        {
            if (!candidate.IsPrimarySurface)
                continue;

            if (candidate.View.WindowToken is null && candidate.View.RootView is null)
                continue;

            AddViewNativeIdentity(candidate.View, tokenHashes, rootHashes, viewHashes);
        }

        return new AndroidModalWindowIdentity(tokenHashes, rootHashes, viewHashes, tokenHashes.Count > 0 || rootHashes.Count > 0 || viewHashes.Count > 0);
    }

    private static AndroidModalWindowIdentity EmptyIdentity(bool isFallback)
    {
        return new AndroidModalWindowIdentity(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), isFallback);
    }

    private static void CollectDialogWindowCandidates(
        FragmentManager? fragmentManager,
        AndroidModalWindowIdentity identity,
        AndroidView activityDecorView,
        List<AndroidModalWindowCandidate> candidates,
        string path,
        int depth)
    {
        if (fragmentManager is null)
            return;

        if (fragmentManager.IsDestroyed)
            return;

        var index = 0;
        foreach (var fragment in GetFragments(fragmentManager))
        {
            var fragmentPath = $"{path}/{index}";
            index++;

            if (fragment is null)
                continue;

            if (!CanInspectFragment(fragment))
                continue;

            if (fragment is DialogFragment dialogFragment)
            {
                var window = dialogFragment.Dialog?.Window;
                if (window is not null)
                {
                    var decorView = window.DecorView;
                    var fragmentTypeName = fragment.GetType().FullName ?? fragment.GetType().Name;
                    candidates.Add(new AndroidModalWindowCandidate(
                        window,
                        dialogFragment,
                        decorView,
                        DialogWindowMatchesModalIdentity(window, identity, activityDecorView),
                        IsMauiModalNavigationFragment(fragmentTypeName),
                        fragmentTypeName,
                        depth,
                        fragmentPath));
                }
            }

            if (!TryGetChildFragmentManager(fragment, out var childFragmentManager))
                continue;

            CollectDialogWindowCandidates(
                childFragmentManager,
                identity,
                activityDecorView,
                candidates,
                fragmentPath,
                depth + 1);
        }
    }

    private static IEnumerable<Fragment?> GetFragments(FragmentManager fragmentManager)
    {
        try
        {
            return fragmentManager.Fragments ?? Enumerable.Empty<Fragment?>();
        }
        catch
        {
            return Enumerable.Empty<Fragment?>();
        }
    }

    private static bool TryGetChildFragmentManager(
        Fragment fragment,
        out FragmentManager? childFragmentManager)
    {
        childFragmentManager = null;

        if (!CanInspectFragment(fragment))
            return false;

        try
        {
            childFragmentManager = fragment.ChildFragmentManager;
            return childFragmentManager is not null && !childFragmentManager.IsDestroyed;
        }
        catch (Java.Lang.IllegalStateException)
        {
            return false;
        }
    }

    private static bool CanInspectFragment(Fragment fragment)
    {
        return fragment.IsAdded &&
               !fragment.IsDetached &&
               !fragment.IsRemoving &&
               fragment.Context is not null &&
               fragment.Activity is not null;
    }

    private static int GetObservedModalStackCount(Component component, Page? mountablePage)
    {
        var modalStackCount = 0;

        static void IncludeNavigation(INavigation? navigation, ref int count)
        {
            if (navigation is not null)
                count = Math.Max(count, navigation.ModalStack.Count);
        }

        var windows = Application.Current?.Windows;
        if (windows is not null)
        {
            foreach (var mauiWindow in windows)
                IncludeNavigation(mauiWindow.Page?.Navigation, ref modalStackCount);
        }

        IncludeNavigation(mountablePage?.Navigation, ref modalStackCount);

        if (component.Presenter is Page presenterPage)
            IncludeNavigation(presenterPage.Navigation, ref modalStackCount);

        if (TryGetNavigationPage(component, mountablePage, out var navigationPage))
            IncludeNavigation(navigationPage.Navigation, ref modalStackCount);

        return modalStackCount;
    }

    private static IEnumerable<NativeViewCandidate> GetComponentNativeViewCandidates(
        Component component,
        Page? mountablePage)
    {
        var seen = new HashSet<IntPtr>();

        if (component.Presenter is Page presenterPage)
        {
            foreach (var candidate in GetNativeViewAndAncestors(
                         $"presenterPage:{presenterPage.GetType().Name}",
                         presenterPage.Handler?.PlatformView,
                         seen))
            {
                yield return candidate;
            }
        }

        if (mountablePage is not null)
        {
            foreach (var candidate in GetNativeViewAndAncestors(
                         $"mountablePage:{mountablePage.GetType().Name}",
                         mountablePage.Handler?.PlatformView,
                         seen))
            {
                yield return candidate;
            }
        }

        if (TryGetNavigationPage(component, mountablePage, out var navigationPage))
        {
            foreach (var candidate in GetNativeViewAndAncestors(
                         $"componentNavigation:{navigationPage.GetType().Name}",
                         navigationPage.Handler?.PlatformView,
                         seen))
            {
                yield return candidate;
            }
        }
    }

    private static bool TryGetNavigationPage(
        Component component,
        Page? mountablePage,
        out NavigationPage navigationPage)
    {
        switch (component)
        {
            case NavigationComponent { Navigation: not null } navigationComponent:
                navigationPage = navigationComponent.Navigation;
                return true;
            case { Presenter: NavigationPage presenterNavigationPage }:
                navigationPage = presenterNavigationPage;
                return true;
            default:
                if (mountablePage is NavigationPage mountableNavigationPage)
                {
                    navigationPage = mountableNavigationPage;
                    return true;
                }

                navigationPage = null!;
                return false;
        }
    }

    private static IEnumerable<NativeViewCandidate> GetNativeViewAndAncestors(
        string source,
        object? platformView,
        HashSet<IntPtr> seen)
    {
        if (platformView is not AndroidView view)
            yield break;

        if (TryAddNativeView(view, seen))
            yield return new NativeViewCandidate($"{source}.platformView", view, true);

        if (view.RootView is AndroidView rootView && TryAddNativeView(rootView, seen))
            yield return new NativeViewCandidate($"{source}.rootView", rootView, true);

        var parent = view.Parent;
        var index = 0;
        while (parent is AndroidView parentView && index < 16)
        {
            if (TryAddNativeView(parentView, seen))
                yield return new NativeViewCandidate($"{source}.parent[{index}]", parentView, false);

            parent = parentView.Parent;
            index++;
        }
    }

    private static bool TryAddNativeView(AndroidView view, HashSet<IntPtr> seen)
    {
        return view.Handle != IntPtr.Zero && seen.Add(view.Handle);
    }

    private static void AddViewNativeIdentity(
        AndroidView view,
        HashSet<int> tokenHashes,
        HashSet<int> rootHashes,
        HashSet<int> viewHashes)
    {
        if (view.WindowToken is not null)
            tokenHashes.Add(view.WindowToken.GetHashCode());

        if (view.RootView is AndroidView rootView)
            rootHashes.Add(rootView.GetHashCode());

        viewHashes.Add(view.GetHashCode());
    }

    private static bool DialogWindowMatchesModalIdentity(
        AndroidWindow window,
        AndroidModalWindowIdentity identity,
        AndroidView activityDecorView)
    {
        var decorView = window.DecorView;
        return decorView is not null &&
               identity.HasCandidates &&
               !IsPrimaryWindowTokenView(decorView, activityDecorView) &&
               (TokenMatchesModalIdentity(decorView, identity) ||
                RootMatchesModalIdentity(decorView, identity) ||
                identity.ContainsViewHash(decorView.GetHashCode()));
    }

    private static bool TokenMatchesModalIdentity(AndroidView view, AndroidModalWindowIdentity identity)
    {
        return view.WindowToken is not null &&
               identity.ContainsTokenHash(view.WindowToken.GetHashCode());
    }

    private static bool RootMatchesModalIdentity(AndroidView view, AndroidModalWindowIdentity identity)
    {
        return view.RootView is AndroidView rootView &&
               identity.ContainsRootHash(rootView.GetHashCode());
    }

    private static bool IsModalTokenView(AndroidView view, AndroidView activityDecorView)
    {
        return view.WindowToken is not null &&
               activityDecorView.WindowToken is not null &&
               !Equals(view.WindowToken, activityDecorView.WindowToken);
    }

    private static bool IsPrimaryWindowTokenView(AndroidView view, AndroidView activityDecorView)
    {
        return view.WindowToken is not null &&
               activityDecorView.WindowToken is not null &&
               Equals(view.WindowToken, activityDecorView.WindowToken);
    }

    private static bool IsMauiModalNavigationFragment(string fragmentTypeName)
    {
        return fragmentTypeName.Contains("ModalNavigationManager", StringComparison.Ordinal) &&
               fragmentTypeName.Contains("ModalFragment", StringComparison.Ordinal);
    }

    private sealed record NativeViewCandidate(string Source, AndroidView View, bool IsPrimarySurface);
}
#endif
