using System;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The "survive the bomb" rules. Two independent paths:
    /// <list type="bullet">
    /// <item>Gold revive — pay to survive. No per-run cap; the price rises with how deep the run got and
    /// doubles every time it is used again in the same run (×1, ×2, ×4 …), so a chain of revives gets
    /// expensive fast.</item>
    /// <item>Ad revive — watch a video to survive. Free, but capped per run (default once).</item>
    /// </list>
    /// </summary>
    public sealed class ContinueService
    {
        // Guards the doubling shift from overflowing a long before the int clamp in CostFor catches it.
        private const int MaxDoublingShift = 40;

        private readonly GoldWallet _wallet;
        private readonly ContinueSettings _settings;

        public ContinueService(GoldWallet wallet, ContinueSettings settings)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _settings = settings;
        }

        /// <summary>
        /// The gold price of the next revive: the depth curve
        /// (<c>BaseCost + CostPerZone * zone</c>) doubled once for each gold revive already taken this run.
        /// </summary>
        public int CostFor(int zoneReached, int goldRevivesUsedThisRun)
        {
            if (zoneReached < 1)
                throw new ArgumentOutOfRangeException(nameof(zoneReached), zoneReached, "Zones are 1-indexed.");
            if (goldRevivesUsedThisRun < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(goldRevivesUsedThisRun), goldRevivesUsedThisRun, "Revive count cannot be negative.");

            long depthCost = (long)_settings.BaseCost + (long)_settings.CostPerZone * zoneReached;
            long cost = depthCost << Math.Min(goldRevivesUsedThisRun, MaxDoublingShift);
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        /// <summary>
        /// The paid revive is offered for as long as the player can afford the (doubling) price — there is
        /// no per-run limit on it.
        /// </summary>
        public bool IsGoldReviveOffered(int zoneReached, int goldRevivesUsedThisRun) =>
            _wallet.CanAfford(CostFor(zoneReached, goldRevivesUsedThisRun));

        /// <summary>
        /// The ad revive is the one free escape, capped per run (default once). No wallet check — watching
        /// the video is the price.
        /// </summary>
        public bool IsAdReviveOffered(int adRevivesUsedThisRun) =>
            adRevivesUsedThisRun < _settings.MaxAdRevivesPerRun;

        /// <summary>Debits the wallet for a gold revive. Returns false and changes nothing when not allowed.</summary>
        public bool TryPurchase(int zoneReached, int goldRevivesUsedThisRun)
        {
            if (!IsGoldReviveOffered(zoneReached, goldRevivesUsedThisRun)) return false;

            return _wallet.TrySpend(CostFor(zoneReached, goldRevivesUsedThisRun));
        }
    }
}
