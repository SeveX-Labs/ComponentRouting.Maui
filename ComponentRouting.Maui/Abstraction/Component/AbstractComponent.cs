using Microsoft.Maui.ApplicationModel;
using NGettext;
using System;
using System.Threading.Tasks;
using ComponentRouting.Maui.Exceptions;
using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class AbstractComponent<TState, TResult> : RoutableComponent<TState, TResult>, LocalizableComponent
    {
        #region auto-properties

        protected TaskCompletionSource<TResult>? CompletionSource { get; private set; }
        protected TState State { get; private set; }

        private bool WasLayoutConfigured { get; set; }

        internal bool HasPendingPresentation =>
            CompletionSource is { } completionSource && !completionSource.Task.IsCompleted;

        #endregion

        #region RoutableComponent implementation

        public Presenter? Presenter { get; private set; }

        public virtual bool CanDispose()
        {
            return true;
        }

        public virtual void Dispose()
        {
            if (!CanDispose()) return;

            if (Presenter is IDisposable disposablePresenter)
                disposablePresenter.Dispose();

            Presenter = null;
            WasLayoutConfigured = false;
        }

        public async Task<Presenter> Prepare(TState? state)
        {
            if (state is null) throw new ComponentException(ComponentError.MissingState);

            State = state;

            await EnsureConfiguredLayout(state);

            await Initialize(state);

            return Presenter;
        }

        public Task<TResult> Present()
        {
            CompletionSource = new TaskCompletionSource<TResult>();

            _ = PresentInternal();

            return CompletionSource.Task;
        }

        public virtual void Resume()
        {

        }

        public virtual bool Unpresent()
        {
            if (!(CompletionSource is null))
            {
                return CompletionSource.TrySetResult(default(TResult));
            }

            return false;
        }

        #endregion

        #region LocalizableElement implementation

        public virtual async Task ApplyLocalization(ICatalog catalog)
        {
            await EnsureLayout();

            var localizableElement = Presenter as LocalizableElement;
            if (!(localizableElement is null))
                await MainThread.InvokeOnMainThreadAsync(() => localizableElement.ApplyLocalization(catalog));
        }

        #endregion

        #region abstract methods

        protected abstract Presenter CreatePresenter();
        protected abstract Task Initialize(TState state);
        protected abstract Task Configure(TState state);
        protected abstract Task PresentInternal();

        #endregion

        #region helper methods

        private async Task EnsureLayout()
        {
            if (Presenter is null)
            {
                Presenter = await MainThread.InvokeOnMainThreadAsync(CreatePresenter); // <- UI thread
            }
        }

        protected async Task EnsureConfiguredLayout(TState state)
        {
            if (!WasLayoutConfigured)
            {
                WasLayoutConfigured = true;

                await EnsureLayout();

                await Configure(state);
            }
        }

        #endregion

    }
}
