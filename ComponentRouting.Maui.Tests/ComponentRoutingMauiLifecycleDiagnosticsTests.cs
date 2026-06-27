using Xunit;

namespace ComponentRouting.Maui.Tests;

public sealed class ComponentRoutingMauiLifecycleDiagnosticsTests : IDisposable
{
    public ComponentRoutingMauiLifecycleDiagnosticsTests()
    {
        ComponentRoutingMauiLifecycleDiagnostics.ResetForTests();
    }

    public void Dispose()
    {
        ComponentRoutingMauiLifecycleDiagnostics.ResetForTests();
    }

    [Fact]
    public void Does_not_warn_when_automatic_platform_lifecycle_is_not_enabled()
    {
        var warnings = new List<string>();
        using var _ = ComponentRoutingMauiLifecycleDiagnostics.UseWarningSinkForTests(warnings.Add);

        ComponentRoutingMauiLifecycleDiagnostics.WarnIfAutomaticPlatformLifecycleEnabledWithoutWindowLifecycle();

        Assert.Empty(warnings);
    }

    [Fact]
    public void Warns_once_when_automatic_platform_lifecycle_is_enabled_without_window_lifecycle()
    {
        var warnings = new List<string>();
        using var _ = ComponentRoutingMauiLifecycleDiagnostics.UseWarningSinkForTests(warnings.Add);
        ComponentRoutingMauiLifecycleDiagnostics.MarkAutomaticPlatformLifecycleEnabled();

        ComponentRoutingMauiLifecycleDiagnostics.WarnIfAutomaticPlatformLifecycleEnabledWithoutWindowLifecycle();
        ComponentRoutingMauiLifecycleDiagnostics.WarnIfAutomaticPlatformLifecycleEnabledWithoutWindowLifecycle();

        var warning = Assert.Single(warnings);
        Assert.Contains("EnableAutomaticPlatformLifecycle() does not replace", warning);
        Assert.Contains("UseComponentRoutingMauiLifecycle", warning);
        Assert.Contains("Window.Created", warning);
        Assert.Contains("Window.Destroying", warning);
    }

    [Fact]
    public void Does_not_warn_when_window_lifecycle_has_been_attached()
    {
        var warnings = new List<string>();
        using var _ = ComponentRoutingMauiLifecycleDiagnostics.UseWarningSinkForTests(warnings.Add);
        ComponentRoutingMauiLifecycleDiagnostics.MarkAutomaticPlatformLifecycleEnabled();
        ComponentRoutingMauiLifecycleDiagnostics.MarkWindowLifecycleAttached();

        ComponentRoutingMauiLifecycleDiagnostics.WarnIfAutomaticPlatformLifecycleEnabledWithoutWindowLifecycle();

        Assert.Empty(warnings);
    }
}
