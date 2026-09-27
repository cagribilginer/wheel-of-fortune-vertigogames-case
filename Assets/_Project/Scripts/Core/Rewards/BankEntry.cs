namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>One stacked row in the run bank: a reward and how much of it the player is holding.</summary>
    public readonly struct BankEntry
    {
        public readonly RewardId Reward;
        public readonly int Amount;

        public BankEntry(RewardId reward, int amount)
        {
            Reward = reward;
            Amount = amount;
        }

        public override string ToString()
        {
            return $"{Reward} x{Amount}";
        }
    }
}
