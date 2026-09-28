using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>The cash-out summary and its claim celebration.</summary>
    public interface ICashOutPresentation
    {
        /// <summary>Shows the live haul and the wallet as it stands before the claim. Nothing is committed yet.</summary>
        void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, WalletBalances wallet);

        /// <summary>Plain dismissal: the player cancelled, so no reward flourish.</summary>
        void HideCashOut();

        /// <summary>
        /// The wallet is already credited: counts the summary up to <paramref name="wallet"/>, then calls
        /// <paramref name="onComplete"/> so the state machine can reset the run.
        /// </summary>
        void ClaimCashOut(WalletBalances wallet, Action onComplete);
    }
}
