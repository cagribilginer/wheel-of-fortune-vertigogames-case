using System;
using System.Collections.Generic;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Owns which state is current and routes player input to it.
    /// <para>
    /// Transitions are drained through a queue rather than recursing: a state that changes state from
    /// inside <see cref="IGameState.Enter"/> (the boot chain does exactly this) would otherwise nest
    /// Exit/Enter calls and make their ordering depend on call depth.
    /// </para>
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();
        private readonly Queue<Type> _pending = new Queue<Type>();
        private bool _draining;

        public IGameState Current { get; private set; }

        public event Action<IGameState> StateChanged;

        public void Register(IGameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            Type key = state.GetType();
            if (_states.ContainsKey(key))
                throw new InvalidOperationException($"State {key.Name} is already registered.");

            _states[key] = state;
        }

        public bool IsIn<TState>() where TState : IGameState => Current is TState;

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
                    StateChanged?.Invoke(Current);
                    Current.Enter();
                }
            }
            finally
            {
                _draining = false;
            }
        }

        // Input surface. Each call is forwarded to the current state, which ignores what it does not accept.
        public void RequestSpin() => Current?.OnSpinRequested();
        public void RequestLeave() => Current?.OnLeaveRequested();
        public void RequestGiveUp() => Current?.OnGiveUpRequested();
        public void Confirm() => Current?.OnConfirmed();
        public void Cancel() => Current?.OnCancelled();
        public void RequestRestart() => Current?.OnRestartRequested();
        public void RequestContinue() => Current?.OnContinueRequested();
    }
}
