using System;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.States.Flow;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Wires the debug cheat bar to the real model and state machine. Only ever constructed in the editor
    /// or a development build (see <see cref="GameInstaller"/>), so the cheats do not need their own
    /// guards — reaching them at all already means debug tooling is on.
    /// </summary>
    public sealed class DebugPresenter
    {
        private const int GoldGrant = 1000;
        private const int ItemGrantCount = 40;

        private readonly RunModel _run;
        private readonly GameStateMachine _machine;
        private readonly GoldWallet _wallet;
        private readonly RewardCatalog _catalog;
        private readonly BankPresenter _bank;
        private readonly Random _rng = new Random();

        public DebugPresenter(
            RunModel run, GameStateMachine machine, GoldWallet wallet, RewardCatalog catalog, BankPresenter bank)
        {
            _run = run;
            _machine = machine;
            _wallet = wallet;
            _catalog = catalog;
            _bank = bank;
        }

        public void WireInput(DebugOverlayView view)
        {
            view.JumpToZone5Clicked += () => JumpToZone(5);
            view.JumpToZone30Clicked += () => JumpToZone(30);
            view.TriggerBombClicked += TriggerBombDefeat;
            view.GrantGoldClicked += () => _wallet.Add(GoldGrant);
            view.GrantItemsClicked += GrantItems;
        }

        // Warping only makes sense between spins; from anywhere else the wheel or a popup owns the screen.
        private void JumpToZone(int zone)
        {
            if (!_machine.IsIn<IdleState>()) return;

            _run.JumpToZone(zone);
            _machine.Change<ZoneSetupState>();
        }

        private void TriggerBombDefeat()
        {
            if (!_machine.IsIn<IdleState>()) return;

            _machine.Change<BombHitState>();
        }

        // Stuffs the run bank with a full, varied haul so the multi-item grid, the defeat-popup scroll and
        // the claim sequence can all be exercised in one press. Only between spins, and the grid is refreshed
        // by hand because nothing else redraws the bank outside of zone setup.
        private void GrantItems()
        {
            if (!_machine.IsIn<IdleState>()) return;

            int count = _catalog.All.Count;
            if (count == 0) return;

            for (int i = 0; i < ItemGrantCount; i++)
            {
                RewardDefinition definition = _catalog.All[i % count];
                if (definition == null) continue;

                _run.Bank.Add(
                    definition.RewardId,
                    definition.DefaultBaseAmount + _rng.Next(0, 40),
                    Math.Max(1, definition.EstimatedValue));
            }

            _bank.Refresh();

            // Re-enter idle so the EXIT button picks up the now non-empty bank without needing a spin first.
            _machine.Change<IdleState>();
        }
    }
}
