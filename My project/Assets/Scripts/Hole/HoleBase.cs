using System;
using UnityEngine;
using VoidEater.Core;
using VoidEater.Objects;
using VoidEater.Utils;

namespace VoidEater.Hole
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class HoleBase : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField, Min(0.1f)] private float radius = 1f;
        [SerializeField] private Transform visualRoot;

        private SphereCollider trigger;
        private Rigidbody body;
        private int score;

        public event Action<float> RadiusChanged;
        public event Action<int> ScoreChanged;
        public event Action<Swallowable> Swallowed;

        public float Radius => radius;
        public int Score => score;
        protected Rigidbody Body => body;

        protected virtual void Awake()
        {
            trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;

            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            ApplyRadius(radius, true);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            Swallowable target = other.GetComponentInParent<Swallowable>();
            TrySwallow(target);
        }

        public bool CanSwallow(Swallowable target)
        {
            if (target == null || target.IsBeingSwallowed)
            {
                return false;
            }

            float margin = settings != null ? settings.swallowMargin : 0.05f;
            return radius > target.Size + margin;
        }

        public bool TrySwallow(Swallowable target)
        {
            return CanSwallow(target) && target.BeginSwallow(this);
        }

        public void NotifySwallowComplete(Swallowable target)
        {
            if (target == null)
            {
                return;
            }

            AddGrowth(target.Volume, target.Score);
            Swallowed?.Invoke(target);
        }

        protected void AddGrowth(float objectVolume, int objectScore)
        {
            float coefficient = settings != null ? settings.growthCoefficient : 0.35f;
            float minimumObjectVolume = settings != null ? settings.minimumObjectVolume : 0.01f;
            float minimumHoleArea = settings != null ? settings.minimumHoleArea : 0.25f;

            float safeObjectVolume = Mathf.Max(minimumObjectVolume, objectVolume);
            float safeHoleArea = Mathf.Max(minimumHoleArea, MathExt.DiskArea(radius));
            float radiusGain = coefficient * Mathf.Sqrt(safeObjectVolume / safeHoleArea);

            ApplyRadius(radius + radiusGain, false);
            score += Mathf.Max(0, objectScore);
            ScoreChanged?.Invoke(score);
        }

        private void ApplyRadius(float nextRadius, bool forceEvent)
        {
            radius = Mathf.Max(0.1f, nextRadius);
            trigger.radius = radius;

            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(radius * 2f, visualRoot.localScale.y, radius * 2f);
            }

            if (forceEvent || RadiusChanged != null)
            {
                RadiusChanged?.Invoke(radius);
            }
        }
    }
}
