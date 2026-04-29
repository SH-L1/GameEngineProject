using UnityEngine;

namespace VoidEater.Core
{
    [CreateAssetMenu(menuName = "Void Eater/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Header("Hole Growth")]
        [Min(0.001f)]
        public float growthCoefficient = 0.35f;

        [Min(0.001f)]
        public float minimumObjectVolume = 0.01f;

        [Min(0.001f)]
        public float minimumHoleArea = 0.25f;

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
    }
}
