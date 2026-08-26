using UnityEngine;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>Cost curve for surviving a bomb. Cheap early, a real decision late.</summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Continue", fileName = "Continue_")]
    public sealed class ContinueConfig : ScriptableObject
    {
        [Min(0)] [SerializeField] private int _baseCost = 50;
        [Min(0)] [SerializeField] private int _costPerZone = 10;
        [Min(0)] [SerializeField] private int _maxContinuesPerRun = 1;

        public ContinueSettings ToSettings() =>
            new ContinueSettings(_baseCost, _costPerZone, _maxContinuesPerRun);
    }
}
