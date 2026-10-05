using System;
using System.Collections.Generic;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Owns the current state and routes input to it. Transitions drain through a queue, so a state that
    /// changes state inside Enter (the boot chain) cannot nest Exit/Enter calls.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new();
        private readonly Queue<Type> _pending = new();
        private bool _draining;

        public IGameState Current { get; private set; }

        public void Register(IGameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            Type key = state.GetType();
            if (_states.ContainsKey(key))
                throw new InvalidOperationException($"State {key.Name} is already registered.");

            _states[key] = state;
        }

        public bool IsIn<TState>() where TState : IGameState
        {
            return Current is TState;
        }

        public void Change<TState>() where TState : IGameState
        {
            Type key = typeof(TState);
            if (!_states.TryGetValue(key, out _))
                throw new InvalidOperationException($"State {key.Name} was never registered.");

            _pending.Enqueue(key);
            if (_draining) return;

            _draining = true;
            try
            {
                while (_pending.Count > 0)
                {
                    IGameState next = _states[_pending.Dequeue()];

                    Current?.Exit();
                    Current = next;
                    Current.Enter();
                }
            }
            finally
            {
                _draining = false;
            }
        }

        // Input surface. Each call goes to the current state only if it declares the matching handler; any other
        // state ignores it on purpose (no second spin while one is running, no confirm outside the summary).
        public void RequestSpin()
        {
            if (Current is ISpinInputHandler handler) handler.OnSpinRequested();
        }
        public void RequestExit()
        {
            if (Current is IExitInputHandler handler) handler.OnExitRequested();
        }
        public void Confirm()
        {
            if (Current is IConfirmInputHandler handler) handler.OnConfirmed();
        }
        public void Cancel()
        {
            if (Current is ICancelInputHandler handler) handler.OnCancelled();
        }
        public void RequestRestart()
        {
            if (Current is IRestartInputHandler handler) handler.OnRestartRequested();
        }
        public void RequestContinue()
        {
            if (Current is IContinueInputHandler handler) handler.OnContinueRequested();
        }
        public void RequestAdContinue()
        {
            if (Current is IAdContinueInputHandler handler) handler.OnAdContinueRequested();
        }
    }
}
