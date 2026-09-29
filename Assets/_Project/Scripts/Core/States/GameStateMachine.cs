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

        // Input surface. Each call is forwarded to the current state, which ignores what it does not accept.
        public void RequestSpin()
        {
            Current?.OnSpinRequested();
        }
        public void RequestExit()
        {
            Current?.OnExitRequested();
        }
        public void Confirm()
        {
            Current?.OnConfirmed();
        }
        public void Cancel()
        {
            Current?.OnCancelled();
        }
        public void RequestRestart()
        {
            Current?.OnRestartRequested();
        }
        public void RequestContinue()
        {
            Current?.OnContinueRequested();
        }
        public void RequestAdContinue()
        {
            Current?.OnAdContinueRequested();
        }
    }
}
