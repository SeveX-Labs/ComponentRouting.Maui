using System;

namespace ComponentRouting.Maui;

public interface ComponentFactory
{
    C CreateComponent<C>() where C : Component;

    Component CreateComponent(Type componentType);
    Component CreateComponent(string componentTypeName);

    void DisposeComponent(Component component);

}