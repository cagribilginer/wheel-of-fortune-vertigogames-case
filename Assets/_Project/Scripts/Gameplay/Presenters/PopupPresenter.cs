using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
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
        private readonly AudioPresenter _audio;
        private readonly JuiceConfig _juice;
        private readonly HaulList _bombHaul;
        private readonly HaulList _collectHaul;
        private readonly CurrencyList _bombCurrencies;
        private readonly CurrencyList _collectCurrencies;
        private GameStateMachine _machine;

        public PopupPresenter(
            BombPopupView bomb, CollectPopupView collect, BankEntryView entryPrefab,
            CurrencyRowView currencyPrefab, RewardCatalog catalog, AudioPresenter audio, JuiceConfig juice)
        {
            _bomb = bomb;
            _collect = collect;
            _audio = audio;
            _juice = juice;
            _bombHaul = new HaulList(entryPrefab, bomb.Content, catalog);
            _collectHaul = new HaulList(entryPrefab, collect.Content, catalog);
            _bombCurrencies = new CurrencyList(currencyPrefab, bomb.CurrencyContent, catalog);
            _collectCurrencies = new CurrencyList(currencyPrefab, collect.CurrencyContent, catalog);
        }

        #region Input
        public void WireInput(GameStateMachine machine)
        {
            _machine = machine;

            // Give up is a restart: the bomb screen raises the same input the old "TRY AGAIN" did.
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
        #endregion

        #region Game over
        public void ShowGameOver(GameOverSummary summary)
        {
            _bombHaul.Show(summary.LostHaul);

            _audio.PlayPopupOpen();
            _audio.PlayDefeatAmbience();

            // The corner HUD shows the actual wallet, not a score built from the lost haul.
            _bombCurrencies.Show(summary.Wallet);
            _bomb.Show(summary);
        }

        public void HideGameOver()
        {
            // No dismiss sting: every route out is a button that already plays the click cue.
            _bomb.Hide();
        }
        #endregion

        #region Cash out
        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, WalletBalances wallet)
        {
            _collectHaul.Show(haul);

            _audio.PlayPopupOpen();

            // The wallet as it stands before this claim lands; ClaimCashOut is what counts it up.
            _collectCurrencies.CountTo(wallet, _juice.CountUpDuration);
            _collect.Show(zonesCleared);
        }

        public void HideCashOut()
        {
            // The corner X only; it already plays the shared click cue via UIButtonPunch.
            _collect.Hide();
        }

        public void ClaimCashOut(WalletBalances wallet, System.Action onComplete)
        {
            // No close sound: the next zone's wheel swoosh fires on the same frame and covers the exit.
            _audio.PlayClaim();
            _collect.PlayClaim(onComplete);
            _collectCurrencies.CountTo(wallet, _juice.CountUpDuration);
        }
        #endregion
    }
}
