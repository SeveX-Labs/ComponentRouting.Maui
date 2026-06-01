using Microsoft.Maui.Controls;
using Xunit;

namespace ComponentRouting.Maui.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MauiApplicationCollection
{
    public const string Name = "Maui application";
}

internal static class MauiApplicationTestHost
{
    private static readonly object Sync = new();

    public static void EnsureApplication()
    {
        lock (Sync)
        {
            _ = Application.Current ?? new Application();
        }
    }
}
