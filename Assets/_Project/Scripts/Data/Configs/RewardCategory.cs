namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Broad grouping of a reward, for pool authoring and icon sizing. It also decides stackability
    /// (see <see cref="RewardDefinition.IsStackable"/>): points, consumables and currencies stack; the rest are unique drops.
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
