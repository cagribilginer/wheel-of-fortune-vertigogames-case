using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;
using Vertigo.Wheel.UI.Views.Popups;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>Bomb, cash-out and give-up-confirm popups: population, chest sizing, and input forwarding.</summary>
    public sealed class PopupPresenter
    {
        // Ascending by total haul value; the highest tier at or below the total wins. Reuses the chest
        // RewardDefinitions' own icons rather than a second set of sprite references.
        private static readonly (long Value, string RewardId)[] ChestTiers =
        {
            (0, "Reward_ChestStandard"),
            (60, "Reward_ChestSilver"),
            (120, "Reward_ChestBig"),
            (150, "Reward_ChestGold"),
            (200, "Reward_ChestSuper"),
        };

        private readonly BombPopupView _bomb;
        private readonly CollectPopupView _collect;
        private readonly GiveUpConfirmPopupView _giveUp;
        private readonly RewardCatalog _catalog;
        private readonly ObjectPool<BankEntryView> _listPool;
        private readonly List<BankEntryView> _activeList = new List<BankEntryView>();

        public PopupPresenter(
            BombPopupView bomb, CollectPopupView collect, GiveUpConfirmPopupView giveUp,
            BankEntryView entryPrefab, RewardCatalog catalog)
        {
            _bomb = bomb;
            _collect = collect;
            _giveUp = giveUp;
            _catalog = catalog;

            _listPool = new ObjectPool<BankEntryView>(
                () => Object.Instantiate(entryPrefab, _collect.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => Object.Destroy(e.gameObject));
        }

        public void WireInput(GameStateMachine machine)
        {
            _bomb.ContinueClicked += machine.RequestContinue;
            _bomb.RestartClicked += machine.RequestRestart;
            _collect.ConfirmClicked += machine.Confirm;
            _giveUp.ConfirmClicked += machine.Confirm;
            _giveUp.CancelClicked += machine.Cancel;
        }

        public void ShowGameOver(int zoneReached, bool continueOffered, int continueCost) =>
            _bomb.Show(zoneReached, continueOffered, continueCost);

        public void HideGameOver() => _bomb.Hide();

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared)
        {
            for (int i = 0; i < _activeList.Count; i++) _listPool.Release(_activeList[i]);
            _activeList.Clear();

            long total = 0;
            for (int i = 0; i < haul.Count; i++)
            {
                total += haul[i].TotalValue;

                BankEntryView entry = _listPool.Get();
                entry.SetEntry(_catalog.IconFor(haul[i].Reward), haul[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _activeList.Add(entry);
            }

            _collect.SetChest(ChestFor(total));
            _collect.Show(zonesCleared);
        }

        public void HideCashOut() => _collect.Hide();

        public void ShowGiveUpConfirm(int rewardsAtStake) => _giveUp.Show(rewardsAtStake);

        public void HideGiveUpConfirm() => _giveUp.Hide();

        private Sprite ChestFor(long totalValue)
        {
            string id = ChestTiers[0].RewardId;
            for (int i = 0; i < ChestTiers.Length; i++)
                if (totalValue >= ChestTiers[i].Value) id = ChestTiers[i].RewardId;

            return _catalog.Find(id)?.Icon;
        }
    }
}
