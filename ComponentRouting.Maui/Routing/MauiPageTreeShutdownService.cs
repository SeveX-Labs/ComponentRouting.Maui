using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Routing;

internal sealed class MauiPageTreeShutdownService
{
    public async Task DisconnectCurrentApplicationPageTreesAsync(
        RouterShutdownContext context,
        ISet<IRouterShutdownAwarePresenter> notifiedPresenters)
    {
        var rootPages = GetCurrentRootPages();
        if (!rootPages.Any())
            return;

        var pages = CollectPages(rootPages);

        await NotifyShutdownAwarePresentersAsync(pages, context, notifiedPresenters);

        foreach (var rootPage in rootPages)
            TryDisconnectHandlersRecursive(rootPage);

        foreach (var page in pages)
            TryDisconnectHandler(page);
    }

    internal static IReadOnlyList<Page> CollectPages(IEnumerable<Page?> rootPages)
    {
        var pages = new List<Page>();
        var seen = new HashSet<Page>(ReferenceEqualityComparer<Page>.Instance);

        foreach (var rootPage in rootPages)
            CollectPage(rootPage, pages, seen);

        return pages;
    }

    private static IReadOnlyList<Page> GetCurrentRootPages()
    {
        var windows = Application.Current?.Windows;
        if (windows is null)
            return Array.Empty<Page>();

        return windows
            .Select(window => window.Page)
            .Where(page => page is not null)
            .Cast<Page>()
            .Distinct(ReferenceEqualityComparer<Page>.Instance)
            .ToList();
    }

    private static void CollectPage(
        Page? page,
        ICollection<Page> pages,
        ISet<Page> seen)
    {
        if (page is null || !seen.Add(page))
            return;

        pages.Add(page);

        switch (page)
        {
            case FlyoutPage flyoutPage:
                CollectPage(flyoutPage.Flyout, pages, seen);
                CollectPage(flyoutPage.Detail, pages, seen);
                break;

            case NavigationPage navigationPage:
                CollectPage(navigationPage.CurrentPage, pages, seen);
                break;

            case TabbedPage tabbedPage:
                foreach (var child in tabbedPage.Children.ToList())
                    CollectPage(child, pages, seen);
                break;

            case Shell shell:
                CollectPage(shell.CurrentPage, pages, seen);
                break;
        }

        CollectNavigationStacks(page, pages, seen);
    }

    private static void CollectNavigationStacks(
        Page page,
        ICollection<Page> pages,
        ISet<Page> seen)
    {
        try
        {
            foreach (var stackPage in page.Navigation.NavigationStack.ToList())
                CollectPage(stackPage, pages, seen);

            foreach (var modalPage in page.Navigation.ModalStack.ToList())
                CollectPage(modalPage, pages, seen);
        }
        catch (ObjectDisposedException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
        catch (InvalidOperationException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#if ANDROID
        catch (Java.Lang.IllegalStateException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#endif
    }

    private static async Task NotifyShutdownAwarePresentersAsync(
        IEnumerable<Page> pages,
        RouterShutdownContext context,
        ISet<IRouterShutdownAwarePresenter> notifiedPresenters)
    {
        foreach (var page in pages)
        {
            if (page is not IRouterShutdownAwarePresenter shutdownAwarePresenter)
                continue;

            if (!notifiedPresenters.Add(shutdownAwarePresenter))
                continue;

            try
            {
                await shutdownAwarePresenter.OnRouterShutdownAsync(context);
            }
            catch (Exception ex)
            {
                ComponentRoutingDiagnostics.WriteException(ex);
            }
        }
    }

    private static void TryDisconnectHandlersRecursive(Page page)
    {
        try
        {
            page.DisconnectHandlers();
        }
        catch (ObjectDisposedException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
        catch (InvalidOperationException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#if ANDROID
        catch (Java.Lang.IllegalStateException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#endif
    }

    private static void TryDisconnectHandler(Page page)
    {
        try
        {
            page.Handler?.DisconnectHandler();
        }
        catch (ObjectDisposedException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
        catch (InvalidOperationException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#if ANDROID
        catch (Java.Lang.IllegalStateException ex)
        {
            ComponentRoutingDiagnostics.WriteException(ex);
        }
#endif
    }
}
