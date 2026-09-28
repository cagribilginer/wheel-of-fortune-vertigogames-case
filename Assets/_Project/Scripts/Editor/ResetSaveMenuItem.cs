using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Clears the persistent wallet so a reviewer can start fresh. An editor menu item rather than in-game UI,
    /// and it deletes only the wallet keys, never PlayerPrefs wholesale. Currencies come from the RewardCatalog.
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
            var keys = new List<string>();
            foreach (RewardId currency in catalog.CurrencyIds)
            {
                string key = Wallet.SaveKeyFor(currency);
                PlayerPrefs.DeleteKey(key);
                keys.Add(key);
            }
            PlayerPrefs.Save();

            Debug.Log($"[Vertigo] Save reset: {keys.Count} wallet key(s) cleared ({string.Join(", ", keys)}). Wallet is back to 0.");
        }
    }
}
