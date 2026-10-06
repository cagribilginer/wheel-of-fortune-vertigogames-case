namespace Vertigo.Wheel.Gameplay
{
    /// <summary>
    /// The Addressables addresses of the ScriptableObject configs <see cref="GameInstallerMono"/> loads. One place, so
    /// moving or renaming a config asset is a one-line change and a typo cannot hide in the composition root.
    /// </summary>
    public static class ConfigAddresses
    {
        private const string SETTINGS = "Configs/Settings/";

        public const string REWARD_CATALOG = SETTINGS + "RewardCatalog";
        public const string WHEEL_SPIN = SETTINGS + "WheelSpin_Default";
        public const string ZONE_PROGRESSION = SETTINGS + "ZoneProgression_Default";
        public const string CONTINUE = SETTINGS + "Continue_Default";
        public const string JUICE = SETTINGS + "Juice_Default";
        public const string AUDIO_LIBRARY = SETTINGS + "AudioLibrary";
    }
}
