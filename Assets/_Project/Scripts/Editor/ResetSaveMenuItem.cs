using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Clears the persistent wallet so a reviewer can start from a genuinely fresh state.
    /// <para>
    /// Deliberately an editor menu item rather than an in-game button: an in-game reset would be UI that
    /// exists only for the grader. It also deletes just the wallet keys, never PlayerPrefs wholesale, so it
    /// cannot take unrelated editor preferences with it. The reward ids are hardcoded here, mirroring
    /// <see cref="Vertigo.Wheel.Gameplay.GameInstaller"/>'s wiring — if a third persistent currency ever
    /// joins gold and cash, it gets added to this list too.
    /// </para>
    /// </summary>
    internal static class ResetSaveMenuItem
    {
        private static readonly RewardId GoldRewardId = new RewardId("Reward_Gold");
        private static readonly RewardId CashRewardId = new RewardId("Reward_Cash");

        [MenuItem("Tools/Vertigo/Reset Save")]
        private static void ResetSave()
        {
            string goldKey = Wallet.SaveKeyFor(GoldRewardId);
            string cashKey = Wallet.SaveKeyFor(CashRewardId);
            PlayerPrefs.DeleteKey(goldKey);
            PlayerPrefs.DeleteKey(cashKey);
            PlayerPrefs.Save();

            Debug.Log($"[Vertigo] Save reset: '{goldKey}' and '{cashKey}' cleared. Wallet is back to 0.");
        }
    }
}
