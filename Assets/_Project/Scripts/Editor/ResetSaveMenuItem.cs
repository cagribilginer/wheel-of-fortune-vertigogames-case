using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Clears the persistent gold wallet so a reviewer can start from a genuinely fresh state.
    /// <para>
    /// Deliberately an editor menu item rather than an in-game button: an in-game reset would be UI that
    /// exists only for the grader. It also deletes just the wallet key, never PlayerPrefs wholesale, so it
    /// cannot take unrelated editor preferences with it.
    /// </para>
    /// </summary>
    internal static class ResetSaveMenuItem
    {
        [MenuItem("Tools/Vertigo/Reset Save")]
        private static void ResetSave()
        {
            PlayerPrefs.DeleteKey(GoldWallet.SaveKey);
            PlayerPrefs.Save();

            Debug.Log($"[Vertigo] Save reset: '{GoldWallet.SaveKey}' cleared. Gold wallet is back to 0.");
        }
    }
}
