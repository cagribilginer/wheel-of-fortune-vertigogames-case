using System;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.States.Flow;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Wires the debug cheat bar to the real model and state machine. Only ever constructed in the editor
    /// or a development build (see <see cref="GameInstallerMono"/>), so the cheats do not need their own
    /// guards — reaching them at all already means debug tooling is on.
    /// </summary>
    public sealed class DebugPresenter : IDisposable
    {
        private const int GOLD_GRANT = 1000;
        private const int SAFE_ZONE_JUMP = 5;
        private const int SUPER_ZONE_JUMP = 30;

        // Debug "+items" adds the catalogue's base amount plus up to this much extra, so the bank shows a mix.
        private const int EXTRA_ITEM_AMOUNT_RANGE = 40;
        private const int ITEM_GRANT_COUNT = 40;

        private readonly RunModel _run;
        private readonly GameStateMachine _machine;
        private readonly Wallet _wallet;
        private readonly RewardId _goldCurrency;
        private readonly RewardCatalog _catalog;
        private readonly BankPresenter _bank;
        private readonly Random _rng = new();

        private DebugOverlayViewMono _view;

        public DebugPresenter(
            RunModel run, GameStateMachine machine, Wallet wallet, RewardId goldCurrency,
            RewardCatalog catalog, BankPresenter bank)
        {
            _run = run;
            _machine = machine;
            _wallet = wallet;
            _goldCurrency = goldCurrency;
            _catalog = catalog;
            _bank = bank;
        }

        #region Input and lifetime
        public void WireInput(DebugOverlayViewMono view)
        {
            _view = view;
            view.JumpToZone5Clicked += JumpToZone5;
            view.JumpToZone30Clicked += JumpToZone30;
            view.TriggerBombClicked += TriggerBombDefeat;
            view.GrantGoldClicked += GrantGold;
            view.GrantItemsClicked += GrantItems;
        }

        public void Dispose()
        {
            if (!_view) return;

            _view.JumpToZone5Clicked -= JumpToZone5;
            _view.JumpToZone30Clicked -= JumpToZone30;
            _view.TriggerBombClicked -= TriggerBombDefeat;
            _view.GrantGoldClicked -= GrantGold;
            _view.GrantItemsClicked -= GrantItems;
        }
        #endregion

        #region Cheats
        private void JumpToZone5()
        {
            JumpToZone(SAFE_ZONE_JUMP);
        }
        private void JumpToZone30()
        {
            JumpToZone(SUPER_ZONE_JUMP);
        }
        private void GrantGold()
        {
            _wallet.Add(_goldCurrency, GOLD_GRANT);
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

        // Fills the bank with a varied haul. Between spins only; the grid is refreshed by hand.
        private void GrantItems()
        {
            if (!_machine.IsIn<IdleState>()) return;

            int count = _catalog.All.Count;
            if (count == 0) return;

            for (int i = 0; i < ITEM_GRANT_COUNT; i++)
            {
                RewardDefinition definition = _catalog.All[i % count];
                if (!definition) continue;

                // Same per-drop rules as a real wheel slice: unique drops are always one, shards stay capped.
                int amount = definition.IsStackable ? definition.DefaultBaseAmount + _rng.Next(0, EXTRA_ITEM_AMOUNT_RANGE) : 1;
                if (definition.MaxAmountPerDrop > 0) amount = Math.Min(amount, definition.MaxAmountPerDrop);

                _run.Bank.Add(definition.RewardId, amount);
            }

            _bank.Refresh();

            // Re-enter idle so the EXIT button picks up the now non-empty bank without needing a spin first.
            _machine.Change<IdleState>();
        }
        #endregion
    }
}
