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
        private const float DefaultInitialRadius = 0.5f;
        private const float LegacyInitialRadius = 1f;

        [SerializeField] private GameSettings settings;
        [SerializeField, Min(0.1f)] private float radius = DefaultInitialRadius;
        [SerializeField] private Transform visualRoot;

        private SphereCollider trigger;
        private Rigidbody body;
        private int score;

        public event Action<float> RadiusChanged;
        public event Action<int> ScoreChanged;
        public event Action<Swallowable> Swallowed;

        public float Radius => GetWorldRadius();
        public int Score => score;
        protected Rigidbody Body => body;

        protected virtual void Awake()
        {
            CacheComponents();
            ConfigurePhysics();
            MigrateLegacyRadius();
            ApplyRadius(radius, true);
        }

        protected virtual void OnValidate()
        {
            MigrateLegacyRadius();
            radius = Mathf.Max(0.1f, radius);

            if (trigger == null)
            {
                trigger = GetComponent<SphereCollider>();
            }

            if (trigger != null)
            {
                trigger.isTrigger = true;
                trigger.radius = radius;
            }
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            Swallowable target = other.GetComponentInParent<Swallowable>();
            if (target == null)
            {
                return;
            }

            target.ApplyHoleInfluence(this, CanSwallow(target));
        }

        public bool CanSwallow(Swallowable target)
        {
            if (target == null || target.IsConsumed)
            {
                return false;
            }

            float tolerance = settings != null ? settings.swallowTolerance : 0.1f;
            float clearance = settings != null ? settings.passThroughClearance : 0.05f;
            if (!target.CanFitThrough(Radius, clearance))
            {
                return false;
            }

            bool requireFullContainment = settings != null && settings.requireFullContainment;
            if (!requireFullContainment)
            {
                return true;
            }

            Vector3 toTarget = target.SwallowCenter - transform.position;
            toTarget.y = 0f;

            return toTarget.magnitude + target.Size <= Radius + tolerance;
        }

        public bool TrySwallow(Swallowable target)
        {
            if (!CanSwallow(target))
            {
                return false;
            }

            target.ApplyHoleInfluence(this, true);
            return true;
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
            float safeHoleArea = Mathf.Max(minimumHoleArea, MathExt.DiskArea(Radius));
            float radiusGain = coefficient * Mathf.Sqrt(safeObjectVolume / safeHoleArea);

            ApplyRadius(radius + radiusGain, false);
            score += Mathf.Max(0, objectScore);
            ScoreChanged?.Invoke(score);
        }

        private void CacheComponents()
        {
            trigger = GetComponent<SphereCollider>();
            body = GetComponent<Rigidbody>();
        }

        private void ConfigurePhysics()
        {
            trigger.isTrigger = true;
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void MigrateLegacyRadius()
        {
            if (Mathf.Approximately(radius, LegacyInitialRadius))
            {
                radius = DefaultInitialRadius;
            }
        }

        private float GetWorldRadius()
        {
            if (trigger != null)
            {
                Bounds bounds = trigger.bounds;
                return Mathf.Max(bounds.extents.x, bounds.extents.z);
            }

            Vector3 scale = transform.lossyScale;
            return radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
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
