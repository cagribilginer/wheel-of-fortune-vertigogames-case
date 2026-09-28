using TMPro;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// How a reward amount reads on screen ("x12"). One place, so the wheel slots, the bank cells and the
    /// editor preview can't drift apart.
    /// </summary>
    public static class AmountFormat
    {
        private const string PATTERN = "x{0}";

        /// <summary>Zero-alloc: TMP formats the integer itself.</summary>
        public static void Apply(TMP_Text label, int amount)
        {
            label.SetText(PATTERN, amount);
        }

        public static string Text(int amount)
        {
            return string.Format(PATTERN, amount);
        }
    }
}
