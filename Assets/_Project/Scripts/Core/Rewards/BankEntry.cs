namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>One stacked row in the run bank: a reward and how much of it the player is holding.</summary>
    public readonly struct BankEntry
    {
        public readonly RewardId Reward;
        public readonly int Amount;
        public readonly int UnitValue;

        public BankEntry(RewardId reward, int amount, int unitValue)
        {
            Reward = reward;
            Amount = amount;
            UnitValue = unitValue;
        }

        /// <summary>Used only to pick which chest sprite the cash-out popup shows.</summary>
        public long TotalValue => (long)Amount * UnitValue;

        public override string ToString() => $"{Reward} x{Amount}";
    }
}
