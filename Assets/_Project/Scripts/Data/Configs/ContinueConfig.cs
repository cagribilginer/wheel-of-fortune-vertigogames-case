using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Cost curve for surviving a bomb. The gold revive is cheap early, a real decision late, and doubles
    /// each time it is used in a run; only the free ad revive is capped per run.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Continue", fileName = "Continue_")]
    public sealed class ContinueConfig : ScriptableObject
    {
        [Min(0)] [SerializeField] private int _baseCost = 50;
        [Min(0)] [SerializeField] private int _costPerZone = 10;

        [Tooltip("Cap on the free ad revive only. The paid gold revive is unlimited (affordability aside).")]
        [Min(0)] [SerializeField] [FormerlySerializedAs("_maxContinuesPerRun")] private int _maxAdRevivesPerRun = 1;

        public ContinueSettings ToSettings() =>
            new ContinueSettings(_baseCost, _costPerZone, _maxAdRevivesPerRun);
    }
}
