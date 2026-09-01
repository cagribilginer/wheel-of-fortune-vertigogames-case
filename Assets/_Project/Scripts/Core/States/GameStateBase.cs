using System;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// No-op defaults for every input, so each state overrides only what it actually accepts.
    /// <para>
    /// That default is load-bearing: a request a state does not override is silently ignored, which is
    /// precisely why double-tapping spin mid-spin cannot queue a second spin.
    /// </para>
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
        protected GameStateBase(GameContext context) =>
            Context = context ?? throw new ArgumentNullException(nameof(context));

        protected GameContext Context { get; }

        protected GameStateMachine Machine => Context.Machine;

        public virtual void Enter() { }
        public virtual void Exit() { }

        public virtual void OnSpinRequested() { }
        public virtual void OnLeaveRequested() { }
        public virtual void OnGiveUpRequested() { }
        public virtual void OnExitRequested() { }
        public virtual void OnConfirmed() { }
        public virtual void OnCancelled() { }
        public virtual void OnRestartRequested() { }
        public virtual void OnContinueRequested() { }
        public virtual void OnAdContinueRequested() { }
    }
}
