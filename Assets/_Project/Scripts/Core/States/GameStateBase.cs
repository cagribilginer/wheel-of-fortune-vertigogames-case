using System;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>Shared plumbing for the states: the context, the machine, and empty lifecycle defaults.</summary>
    public abstract class GameStateBase : IGameState
    {
        protected GameStateBase(GameContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected GameContext Context { get; }

        protected GameStateMachine Machine
        {
            get { return Context.Machine; }
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
    }
}
