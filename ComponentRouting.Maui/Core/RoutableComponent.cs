using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface RoutableComponent<TState, TResult> : Component
{
    Task<Presenter> Prepare(TState state);
    Task<TResult> Present();
}