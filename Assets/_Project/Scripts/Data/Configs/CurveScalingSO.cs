using System;
using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Designer-drawn growth curve.
    /// <para>
    /// This one has to live on the Unity side: AnimationCurve does not exist in the engine-free core, so
    /// the curve is evaluated here and only an int crosses the boundary.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Scaling/Curve", fileName = "Scaling_Curve")]
    public sealed class CurveScalingSO : ScalingStrategySO
    {
        [Tooltip("X is zone normalised over Max Zone; Y is the multiplier applied to the base amount.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 1f, 1f, 10f);

        [Min(1)] [SerializeField] private int _maxZone = 100;

        public override int Scale(int baseAmount, int zone)
        {
            if (zone < 1) throw new ArgumentOutOfRangeException(nameof(zone), zone, "Zones are 1-indexed.");
            if (baseAmount < 1) throw new ArgumentOutOfRangeException(nameof(baseAmount), baseAmount, "Base amount must be >= 1.");

            float t = Mathf.Clamp01((zone - 1) / (float)_maxZone);
            float multiplier = Mathf.Max(1f, _curve.Evaluate(t));

            double scaled = Math.Ceiling(baseAmount * (double)multiplier);
            return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
        }
    }
}
