using System.Collections.Generic;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>Non-persistent <see cref="ISaveService"/> for unit tests and editor previews.</summary>
    public sealed class InMemorySaveService : ISaveService
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

        public int GetInt(string key, int defaultValue = 0) =>
            _values.TryGetValue(key, out int value) ? value : defaultValue;

        public void SetInt(string key, int value) => _values[key] = value;

        public void Save() { /* nothing to flush */ }
    }
}
