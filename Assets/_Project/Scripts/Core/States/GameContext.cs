using System;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// The shared services and per-spin scratch data the states operate on.
    /// Passed in by the composition root, so no state ever reaches for a singleton.
    /// </summary>
    public sealed class GameContext
    {
        public GameContext(
            RunModel run,
            ZoneWheelFactory wheelFactory,
            SpinService spinService,
            ContinueService continueService,
            IWheelPresentation presentation)
        {
            Run = run ?? throw new ArgumentNullException(nameof(run));
            WheelFactory = wheelFactory ?? throw new ArgumentNullException(nameof(wheelFactory));
            SpinService = spinService ?? throw new ArgumentNullException(nameof(spinService));
            ContinueService = continueService ?? throw new ArgumentNullException(nameof(continueService));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));

            Zone = presentation;
            Input = presentation;
            Spin = presentation;
            GameOver = presentation;
            CashOut = presentation;
        }

        public RunModel Run { get; }
        public ZoneWheelFactory WheelFactory { get; }
        public SpinService SpinService { get; }
        public ContinueService ContinueService { get; }

        // One screen behind the composition root, seen through the narrow role each state actually needs.
        public IZonePresentation Zone { get; }
        public IInputPresentation Input { get; }
        public ISpinPresentation Spin { get; }
        public IGameOverPresentation GameOver { get; }
        public ICashOutPresentation CashOut { get; }

        public GameStateMachine Machine { get; internal set; }

        /// <summary>The wheel currently on screen. Rebuilt on every zone change.</summary>
        public WheelModel CurrentWheel { get; internal set; }

        /// <summary>The result of the spin in flight, decided before the wheel started turning.</summary>
        public SpinOutcome PendingOutcome { get; internal set; }
    }
}
