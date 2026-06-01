using UnityEngine;

namespace VoidEater.Core
{
    [CreateAssetMenu(menuName = "Void Eater/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Header("Hole Growth")]
        [Min(1)]
        public int baseGrowthUnitsRequired = 5;

        [Min(0)]
        public int growthUnitsRequiredIncrease = 2;

        [Min(0.001f)]
        public float smallObjectRadiusLimit = 0.55f;

        [Min(0.001f)]
        public float mediumObjectRadiusLimit = 1.25f;

        [Min(0.001f)]
        public float radiusGainPerLevel = 0.25f;

        [Min(0.1f)]
        public float maximumRadius = 12f;

        [Header("Score")]
        [Min(0f)]
        public float scoreMultiplier = 1f;

        [Tooltip("Extra radius required before a hole can swallow an object.")]
        [Min(0f)]
        public float swallowMargin = 0.05f;

        [Header("Swallow Check")]
        public bool requireFullContainment = false;

        [Tooltip("Small forgiveness value for objects sitting exactly on the hole edge.")]
        [Min(0f)]
        public float swallowTolerance = 0.1f;

        [Tooltip("Extra radius required before an object's footprint can fall fully through the hole.")]
        [Min(0f)]
        public float passThroughClearance = 0.05f;

        public int CalculateGrowthUnits(float requiredPassThroughRadius)
        {
            if (requiredPassThroughRadius <= smallObjectRadiusLimit)
            {
                return 1;
            }

            if (requiredPassThroughRadius <= mediumObjectRadiusLimit)
            {
                return 2;
            }

            return 3;
        }

        public float CalculateGrowthRequired(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return baseGrowthUnitsRequired + growthUnitsRequiredIncrease * (safeLevel - 1);
        }

        public int CalculateScoreGain(int baseScore)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseScore * scoreMultiplier));
        }
    }
}
