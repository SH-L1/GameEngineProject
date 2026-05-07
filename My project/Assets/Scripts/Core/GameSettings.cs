using UnityEngine;

namespace VoidEater.Core
{
    [CreateAssetMenu(menuName = "Void Eater/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Header("Hole Growth")]
        [Min(0.001f)]
        public float baseGrowthRequired = 35f;

        [Min(1f)]
        public float growthRequirementMultiplier = 1.22f;

        [Min(0f)]
        public float objectVolumeProgressWeight = 8f;

        [Min(0f)]
        public float objectScoreProgressWeight = 1f;

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

        public float CalculateGrowthProgress(float objectVolume, int baseScore)
        {
            float volumeProgress = Mathf.Sqrt(Mathf.Max(0f, objectVolume)) * objectVolumeProgressWeight;
            float scoreProgress = Mathf.Max(0, baseScore) * objectScoreProgressWeight;
            return volumeProgress + scoreProgress;
        }

        public float CalculateGrowthRequired(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return baseGrowthRequired * Mathf.Pow(growthRequirementMultiplier, safeLevel - 1);
        }

        public int CalculateScoreGain(int baseScore)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseScore * scoreMultiplier));
        }
    }
}
