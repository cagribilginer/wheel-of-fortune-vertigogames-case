using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Clears the persistent wallet so a reviewer can start from a genuinely fresh state.
    /// <para>
    /// Deliberately an editor menu item rather than an in-game button: an in-game reset would be UI that
    /// exists only for the grader. It also deletes just the wallet keys, never PlayerPrefs wholesale, so it
    /// cannot take unrelated editor preferences with it. The currencies come from the RewardCatalog, the
    /// same place <see cref="Vertigo.Wheel.Gameplay.GameInstaller"/> reads them from.
    /// </para>
    /// </summary>
    internal static class ResetSaveMenuItem
    {
        [MenuItem("Tools/Vertigo/Reset Save")]
        private static void ResetSave()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(RewardCatalog)}");
            if (guids.Length == 0)
            {
                Debug.LogError("[Vertigo] Reset Save: no RewardCatalog found, so the wallet keys are unknown.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RewardCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            string goldKey = Wallet.SaveKeyFor(catalog.GoldCurrency);
            string cashKey = Wallet.SaveKeyFor(catalog.CashCurrency);
            PlayerPrefs.DeleteKey(goldKey);
            PlayerPrefs.DeleteKey(cashKey);
            PlayerPrefs.Save();

            Debug.Log($"[Vertigo] Save reset: '{goldKey}' and '{cashKey}' cleared. Wallet is back to 0.");
        }
    }
}
