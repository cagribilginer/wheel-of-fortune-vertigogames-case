namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// Minimal persistence seam. Backed by PlayerPrefs in the player and by an in-memory dictionary in tests.
    /// <para>
    /// Deliberately three methods rather than a generic repository: the only thing that survives a run is
    /// the gold wallet, and a repository abstraction here would be a pattern in search of a problem.
    /// </para>
    /// </summary>
    public interface ISaveService
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        void Save();
    }
}
