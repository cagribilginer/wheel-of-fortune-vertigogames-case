namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>One stacked row: a reward and how much of it is held, in the run bank or in the wallet.</summary>
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
