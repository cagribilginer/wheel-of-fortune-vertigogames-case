namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// Minimal persistence seam: PlayerPrefs in the player, an in-memory dictionary in tests. Three methods
    /// on purpose, since the wallet is the only thing that survives a run.
    /// </summary>
    public interface ISaveService
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        void Save();
    }
}
