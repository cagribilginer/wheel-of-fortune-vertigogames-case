namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The copy the views write at runtime, in one place. Purely static labels (EXIT, GIVE UP...) live in the scene;
    /// these are the ones a view has to compose or swap. <c>{0}</c> is a TMP format slot filled without allocating.
    /// </summary>
    public static class ViewText
    {
        /// <summary>A bare zone number: the strip tiles and the milestone badges, whose labels are static scene text.</summary>
        public const string ZONE_NUMBER = "{0}";

        public const string SUPER_ZONE_TITLE = "SUPER ZONE";
        public const string SAFE_ZONE_TITLE = "SAFE ZONE";
        public const string SUPER_ZONE_DESCRIPTION = "Win super rewards in bomb-free Super Zones!";
        public const string SAFE_ZONE_DESCRIPTION = "Win special rewards in bomb-free Safe Zones!";

        public const string BOMB_ZONE_REACHED = "You reached Zone {0}";
        public const string COLLECT_ZONES_CLEARED = "Cleared {0} zones";

        /// <summary>Thousands-separated integer, for currency amounts.</summary>
        public const string AMOUNT_FORMAT = "N0";
    }
}
