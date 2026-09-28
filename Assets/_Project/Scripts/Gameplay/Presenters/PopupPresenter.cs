using System;
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
    /// <summary>Bomb and cash-out popups: population and input forwarding.</summary>
    public sealed class PopupPresenter : IDisposable
    {
        private readonly BombPopupView _bomb;
        private readonly CollectPopupView _collect;
        private readonly RewardCatalog _catalog;
        private readonly AudioPresenter _audio;
        private readonly ObjectPool<BankEntryView> _listPool;
        private readonly List<BankEntryView> _activeList = new List<BankEntryView>();
        private readonly ObjectPool<BankEntryView> _bombListPool;
        private readonly List<BankEntryView> _activeBombList = new List<BankEntryView>();
        private GameStateMachine _machine;

        public PopupPresenter(
            BombPopupView bomb, CollectPopupView collect, BankEntryView entryPrefab,
            RewardCatalog catalog, AudioPresenter audio)
        {
            _bomb = bomb;
            _collect = collect;
            _catalog = catalog;
            _audio = audio;

            _listPool = new ObjectPool<BankEntryView>(
                () => UnityEngine.Object.Instantiate(entryPrefab, _collect.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => UnityEngine.Object.Destroy(e.gameObject));

            _bombListPool = new ObjectPool<BankEntryView>(
                () => UnityEngine.Object.Instantiate(entryPrefab, _bomb.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => UnityEngine.Object.Destroy(e.gameObject));
        }

        public void WireInput(GameStateMachine machine)
        {
            _machine = machine;

            // "Give up" forfeits the haul and drops back to zone one — the machine already models that as a
            // restart, so the bomb screen's give-up button raises the same input the old "TRY AGAIN" did.
            _bomb.GiveUpClicked += machine.RequestRestart;
            _bomb.ContinueClicked += machine.RequestContinue;
            _bomb.AdContinueClicked += machine.RequestAdContinue;
            _collect.ConfirmClicked += machine.Confirm;
            _collect.CancelClicked += machine.Cancel;
        }

        public void Dispose()
        {
            if (_machine == null) return;

            _bomb.GiveUpClicked -= _machine.RequestRestart;
            _bomb.ContinueClicked -= _machine.RequestContinue;
            _bomb.AdContinueClicked -= _machine.RequestAdContinue;
            _collect.ConfirmClicked -= _machine.Confirm;
            _collect.CancelClicked -= _machine.Cancel;
        }

        public void ShowGameOver(GameOverSummary summary)
        {
            for (int i = 0; i < _activeBombList.Count; i++) _bombListPool.Release(_activeBombList[i]);
            _activeBombList.Clear();

            IReadOnlyList<BankEntry> lostHaul = summary.LostHaul;
            for (int i = 0; i < lostHaul.Count; i++)
            {
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

            // The corner HUD shows the actual wallet — the same two numbers ShowCashOut shows — not a
            // score built from the lost haul's value. The view stays Core-agnostic, so the summary is
            // unpacked into plain values here rather than passed through.
            _bomb.Show(
                summary.ZoneReached, lostHaul.Count, summary.PlayerCash, summary.PlayerGold,
                summary.GoldReviveOffered, summary.GoldReviveCost, summary.AdReviveOffered);
        }

        public void HideGameOver()
        {
            // No dismiss sting here: every route out is an action button, and each already fires the
            // shared button-click cue via UIButtonPunch.
            _bomb.Hide();
        }

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, int playerGold, int playerCash)
        {
            for (int i = 0; i < _activeList.Count; i++) _listPool.Release(_activeList[i]);
            _activeList.Clear();

            for (int i = 0; i < haul.Count; i++)
            {
                BankEntryView entry = _listPool.Get();
                entry.SetEntry(_catalog.IconFor(haul[i].Reward), haul[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _activeList.Add(entry);
            }

            // Resolve the grid + ContentSizeFitter now so the ScrollRect sees the real content height on
            // the frame the cash-out summary opens.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_collect.Content);

            _audio.PlayPopupOpen();
            _collect.Show(zonesCleared, playerCash, playerGold);
        }

        public void HideCashOut()
        {
            // Dismissing the cash-out summary is the corner X only, and it already fires the shared
            // button-click cue via UIButtonPunch — exactly what the Safe/Super milestone popup's close X
            // plays, so the two dismiss the same way.
            _collect.Hide();
        }

        public void ClaimCashOut(int playerGold, int playerCash, System.Action onComplete)
        {
            // No close sound when the popup goes: onComplete starts the next zone, whose wheel-transition
            // swoosh fires on the same frame and already covers the exit.
            _audio.PlayClaim();
            _collect.PlayClaim(playerCash, playerGold, onComplete);
        }
    }
}
