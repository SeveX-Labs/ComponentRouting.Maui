using ComponentRouting.Maui.Exceptions;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class ExceptionCharacterizationTests
{
    [Fact]
    public void RouterError_includes_component_presentation_and_dismissal_errors()
    {
        Assert.Contains(nameof(RouterError.ComponentPresentationFailed), Enum.GetNames<RouterError>());
        Assert.Contains(nameof(RouterError.ComponentDismissalNotSupported), Enum.GetNames<RouterError>());
    }

    [Fact]
    public void ComponentError_includes_missing_state_and_generic_error()
    {
        Assert.Contains(nameof(ComponentError.MissingState), Enum.GetNames<ComponentError>());
        Assert.Contains(nameof(ComponentError.GenericError), Enum.GetNames<ComponentError>());
    }

    [Fact]
    public void RouterException_message_includes_error_and_component_name()
    {
        var component = new TestComponent();
        var exception = new RouterException(RouterError.ComponentPresentationFailed, component);

        Assert.Equal(RouterError.ComponentPresentationFailed, exception.Error);
        Assert.Same(component, exception.Component);
        Assert.Contains(nameof(RouterError.ComponentPresentationFailed), exception.Message);
        Assert.Contains(nameof(TestComponent), exception.Message);
    }
}
