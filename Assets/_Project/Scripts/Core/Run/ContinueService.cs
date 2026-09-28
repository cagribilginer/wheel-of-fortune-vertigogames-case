using System;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The "survive the bomb" rules. Gold revive: a price that grows with depth and doubles on each reuse
    /// in a run. Ad revive: free, capped per run (default once).
    /// </summary>
    public sealed class ContinueService
    {
        // Guards the doubling shift from overflowing a long before the int clamp in CostFor catches it.
        private const int MAX_DOUBLING_SHIFT = 40;

        private readonly Wallet _wallet;
        private readonly RewardId _currency;
        private readonly ContinueSettings _settings;

        public ContinueService(Wallet wallet, RewardId currency, ContinueSettings settings)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _currency = currency;
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
            long cost = depthCost << Math.Min(goldRevivesUsedThisRun, MAX_DOUBLING_SHIFT);
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        /// <summary>
        /// The paid revive is offered for as long as the player can afford the (doubling) price — there is
        /// no per-run limit on it.
        /// </summary>
        public bool IsGoldReviveOffered(int zoneReached, int goldRevivesUsedThisRun)
        {
            return _wallet.CanAfford(_currency, CostFor(zoneReached, goldRevivesUsedThisRun));
        }

        /// <summary>
        /// The ad revive is the one free escape, capped per run (default once). No wallet check — watching
        /// the video is the price.
        /// </summary>
        public bool IsAdReviveOffered(int adRevivesUsedThisRun)
        {
            return adRevivesUsedThisRun < _settings.MaxAdRevivesPerRun;
        }

        /// <summary>Debits the wallet for a gold revive. Returns false and changes nothing when not allowed.</summary>
        public bool TryPurchase(int zoneReached, int goldRevivesUsedThisRun)
        {
            if (!IsGoldReviveOffered(zoneReached, goldRevivesUsedThisRun)) return false;

            return _wallet.TrySpend(_currency, CostFor(zoneReached, goldRevivesUsedThisRun));
        }
    }
}
