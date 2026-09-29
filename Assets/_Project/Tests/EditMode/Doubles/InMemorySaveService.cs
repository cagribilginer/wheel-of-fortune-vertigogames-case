using System.Collections.Generic;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>Non-persistent <see cref="ISaveService"/> double, so a test's wallet never touches PlayerPrefs.</summary>
    public sealed class InMemorySaveService : ISaveService
    {
        private readonly Dictionary<string, int> _values = new();

        public int GetInt(string key, int defaultValue = 0)
        {
            return _values.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _values[key] = value;
        }

        public void Save() { /* nothing to flush */ }
    }
}
