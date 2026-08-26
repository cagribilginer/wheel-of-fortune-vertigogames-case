using System;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The optional "pay to survive the bomb" rule. Cost rises with how deep the run got, so a continue is
    /// cheap early and a real decision late.
    /// </summary>
    public sealed class ContinueService
    {
        private readonly GoldWallet _wallet;
        private readonly ContinueSettings _settings;

        public ContinueService(GoldWallet wallet, ContinueSettings settings)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _settings = settings;
        }

        public int CostFor(int zoneReached)
        {
            if (zoneReached < 1)
                throw new ArgumentOutOfRangeException(nameof(zoneReached), zoneReached, "Zones are 1-indexed.");

            long cost = (long)_settings.BaseCost + (long)_settings.CostPerZone * zoneReached;
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        public bool IsOffered(int zoneReached, int continuesUsedThisRun)
        {
            if (continuesUsedThisRun >= _settings.MaxContinuesPerRun) return false;

            return _wallet.CanAfford(CostFor(zoneReached));
        }

        /// <summary>Debits the wallet. Returns false and changes nothing when the purchase is not allowed.</summary>
        public bool TryPurchase(int zoneReached, int continuesUsedThisRun)
        {
            if (!IsOffered(zoneReached, continuesUsedThisRun)) return false;

            return _wallet.TrySpend(CostFor(zoneReached));
        }
    }
}
