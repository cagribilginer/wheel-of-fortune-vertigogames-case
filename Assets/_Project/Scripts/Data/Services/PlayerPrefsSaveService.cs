using UnityEngine;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>Player-side <see cref="ISaveService"/>. The wallet is the only thing that persists.</summary>
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
