namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The persistent gold and cash balances as one value. Passed through the presentation seam whole,
    /// because two loose ints of the same type are exactly the pair a call site can transpose without the
    /// compiler noticing.
    /// </summary>
    public readonly struct WalletBalances
    {
        public readonly int Gold;
        public readonly int Cash;

        public WalletBalances(int gold, int cash)
        {
            Gold = gold;
            Cash = cash;
        }
    }
}
