using System;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class PushableComponent<TState, TResult> : AbstractComponent<TState, TResult>
    {
        #region abstract methods

        public abstract void HandleBackTapped();

        #endregion

        #region methods implementation

        public override async void Dispose()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => CompletionSource?.TrySetResult(default(TResult)));
            }
            catch (Exception ex)
            {
                ComponentRoutingDiagnostics.WriteException(ex);
            }

            base.Dispose();
        }

        protected override Task Configure(TState state)
        {
            if (Presenter is PushablePresenter pushable)
            {
                pushable.Configure(HandleBackTapped);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
