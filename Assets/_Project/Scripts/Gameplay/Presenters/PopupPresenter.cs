using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
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
        private readonly AudioPresenter _audio;
        private readonly ObjectPool<BankEntryView> _listPool;
        private readonly List<BankEntryView> _activeList = new List<BankEntryView>();
        private readonly ObjectPool<BankEntryView> _bombListPool;
        private readonly List<BankEntryView> _activeBombList = new List<BankEntryView>();

        public PopupPresenter(
            BombPopupView bomb, CollectPopupView collect, GiveUpConfirmPopupView giveUp,
            BankEntryView entryPrefab, RewardCatalog catalog, AudioPresenter audio)
        {
            _bomb = bomb;
            _collect = collect;
            _giveUp = giveUp;
            _catalog = catalog;
            _audio = audio;

            _listPool = new ObjectPool<BankEntryView>(
                () => Object.Instantiate(entryPrefab, _collect.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => Object.Destroy(e.gameObject));

            _bombListPool = new ObjectPool<BankEntryView>(
                () => Object.Instantiate(entryPrefab, _bomb.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => Object.Destroy(e.gameObject));
        }

        public void WireInput(GameStateMachine machine)
        {
            // "Give up" forfeits the haul and drops back to zone one — the machine already models that as a
            // restart, so the bomb screen's give-up button raises the same input the old "TRY AGAIN" did.
            _bomb.GiveUpClicked += machine.RequestRestart;
            _bomb.ContinueClicked += machine.RequestContinue;
            _bomb.AdContinueClicked += machine.RequestAdContinue;
            _collect.ConfirmClicked += machine.Confirm;
            _collect.CancelClicked += machine.Cancel;
            _giveUp.ConfirmClicked += machine.Confirm;
            _giveUp.CancelClicked += machine.Cancel;
        }

        public void ShowGameOver(
            int zoneReached, IReadOnlyList<BankEntry> lostHaul, int playerGold,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered)
        {
            for (int i = 0; i < _activeBombList.Count; i++) _bombListPool.Release(_activeBombList[i]);
            _activeBombList.Clear();

            long lostValue = 0;
            for (int i = 0; i < lostHaul.Count; i++)
            {
                lostValue += lostHaul[i].TotalValue;

                BankEntryView entry = _bombListPool.Get();
                entry.SetEntry(_catalog.IconFor(lostHaul[i].Reward), lostHaul[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _activeBombList.Add(entry);
            }

            // Resolve the horizontal row now so the ScrollRect knows its content width before the popup
            // opens and accepts a swipe on the first frame.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bomb.Content);

            _audio.PlayPopupOpen();
            _audio.PlayDefeatAmbience();

            // The corner HUD shows two numbers: "cash" is the worth of the haul now on the line, "gold" is
            // the persistent wallet a paid revive spends from.
            int lostCash = lostValue > int.MaxValue ? int.MaxValue : (int)lostValue;
            _bomb.Show(
                zoneReached, lostHaul.Count, lostCash, playerGold,
                goldReviveOffered, goldReviveCost, adReviveOffered);
        }

        public void HideGameOver()
        {
            _audio.PlayPopupClose();
            _bomb.Hide();
        }

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

            // Resolve the grid + ContentSizeFitter now so the ScrollRect sees the real content height on
            // the frame the cash-out summary opens.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_collect.Content);

            _collect.SetChest(ChestFor(total));
            _audio.PlayPopupOpen();
            _collect.Show(zonesCleared);
        }

        public void HideCashOut()
        {
            _audio.PlayPopupClose();
            _collect.Hide();
        }

        public void ClaimCashOut(System.Action onComplete)
        {
            _audio.PlayClaim();
            _collect.PlayClaim(() =>
            {
                _audio.PlayPopupClose();
                onComplete();
            });
        }

        public void ShowGiveUpConfirm(int rewardsAtStake)
        {
            _audio.PlayPopupOpen();
            _giveUp.Show(rewardsAtStake);
        }

        public void HideGiveUpConfirm()
        {
            _audio.PlayPopupClose();
            _giveUp.Hide();
        }

        private Sprite ChestFor(long totalValue)
        {
            string id = ChestTiers[0].RewardId;
            for (int i = 0; i < ChestTiers.Length; i++)
                if (totalValue >= ChestTiers[i].Value) id = ChestTiers[i].RewardId;

            return _catalog.Find(id)?.Icon;
        }
    }
}
