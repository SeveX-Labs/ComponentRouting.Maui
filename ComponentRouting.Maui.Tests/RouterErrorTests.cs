using ComponentRouting.Maui.Exceptions;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterErrorTests
{
    [Fact]
    public void RouterError_includes_ComponentPresentationFailed()
    {
        Assert.Contains(
            "ComponentPresentationFailed",
            Enum.GetNames<RouterError>());
    }

    [Fact]
    public void RouterError_includes_ComponentDismissalNotSupported()
    {
        Assert.Contains(
            "ComponentDismissalNotSupported",
            Enum.GetNames<RouterError>());
    }
}
