namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Broad grouping of a reward, used for pool authoring and for icon sizing conventions. It also
    /// decides stackability (see <see cref="RewardDefinition.IsStackable"/>): <see cref="Points"/> are
    /// craft shards, so they stack and scale like <see cref="Consumable"/> and <see cref="Currency"/>;
    /// a <see cref="Weapon"/>, <see cref="Cosmetic"/> or <see cref="Chest"/> is a single unique drop.
    /// </summary>
    public enum RewardCategory
    {
        Points = 0,
        Weapon = 1,
        Consumable = 2,
        Cosmetic = 3,
        Currency = 4,
        Chest = 5
    }
}
