using ComponentRouting.Maui.Extension;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class ReflectionExtensionTests
{
    [Fact]
    public void IsSubclassOfRawGeneric_returns_true_for_indirect_generic_base()
    {
        var instance = new DerivedGenericComponent();

        Assert.True(instance.IsSubclassOfRawGeneric(typeof(GenericComponent<>)));
    }

    [Fact]
    public void IsSubclassOfRawGeneric_returns_false_when_generic_base_is_absent()
    {
        var instance = new TestComponent();

        Assert.False(instance.IsSubclassOfRawGeneric(typeof(GenericComponent<>)));
    }

    [Fact]
    public void GetPropertyInfo_finds_private_instance_property()
    {
        var instance = new PrivatePropertyHolder();

        var propertyInfo = instance.GetPropertyInfo("Secret");

        Assert.NotNull(propertyInfo);
        Assert.Equal("value", propertyInfo.GetValue(instance));
    }

    private sealed class PrivatePropertyHolder
    {
        private string Secret { get; } = "value";
    }
}
