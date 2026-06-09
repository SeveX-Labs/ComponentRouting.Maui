using System;

namespace ComponentRouting.Maui;

public interface Component : IDisposable
{
    Presenter? Presenter { get; }

    void Resume();
    bool Unpresent();
}