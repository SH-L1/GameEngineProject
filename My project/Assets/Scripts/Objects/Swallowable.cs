using UnityEngine;
using VoidEater.Hole;
using VoidEater.Utils;

namespace VoidEater.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Swallowable : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float size;
        [SerializeField, Min(0f)] private int score = 10;

        [Header("Hole Physics")]
        [SerializeField] private bool activatePhysicsOnStart;
        [SerializeField, Min(0f)] private float inwardAcceleration = 18f;
        [SerializeField, Min(0f)] private float edgeDownAcceleration = 34f;
        [SerializeField, Min(0f)] private float tumbleTorque = 18f;
        [SerializeField, Range(0f, 1f)] private float oversizedInfluenceMultiplier = 0.25f;
        [SerializeField, Range(0f, 1f)] private float passThroughSizeFactor = 0.95f;
        [SerializeField, Min(0.05f)] private float consumeDepth = 2.5f;
        [SerializeField, Min(0.05f)] private float influenceMemory = 0.15f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 9f;

        private Collider[] cachedColliders;
        private bool[] initialTriggerStates;
        private Rigidbody body;
        private Vector3 initialScale;
        private float cachedVolume = -1f;
        private HoleBase influencingHole;
        private bool canCompleteSwallow;
        private bool passThroughEnabled;
        private bool isConsumed;
        private float lastInfluenceTime;

        public float Size => size > 0f ? size : EstimateSize();
        public int Score => score;
        public float Volume => cachedVolume > 0f ? cachedVolume : EstimateVolume();
        public bool IsBeingSwallowed => passThroughEnabled && !isConsumed;
        public bool IsConsumed => isConsumed;
        public float RequiredPassThroughRadius => CalculateRequiredPassThroughRadius();
        public Vector3 SwallowCenter => body != null ? body.worldCenterOfMass : CalculateWorldBounds().center;

        private void Awake()
        {
            CacheComponents();
            initialScale = transform.localScale;
            ConfigureBody();
            RecalculateMetrics();
        }

        private void OnEnable()
        {
            if (cachedColliders == null || body == null)
            {
                CacheComponents();
            }

            ResetForReuse();
        }

        private void OnValidate()
        {
            consumeDepth = Mathf.Max(0.05f, consumeDepth);
            influenceMemory = Mathf.Max(0.05f, influenceMemory);
        }

        private void FixedUpdate()
        {
            if (influencingHole == null || isConsumed)
            {
                return;
            }

            bool stillTouchingHole = Time.time - lastInfluenceTime <= influenceMemory;
            if (!stillTouchingHole && !passThroughEnabled)
            {
                influencingHole = null;
                canCompleteSwallow = false;
                return;
            }

            ApplyHoleForces(influencingHole, canCompleteSwallow);

            if (canCompleteSwallow && !passThroughEnabled && IsReadyToFallThrough(influencingHole))
            {
                SetPassThrough(true);
            }

            if (passThroughEnabled && HasFallenThrough(influencingHole))
            {
                CompleteSwallow(influencingHole);
            }
        }

        public void ApplyHoleInfluence(HoleBase hole, bool canBeConsumed)
        {
            if (hole == null || isConsumed)
            {
                return;
            }

            influencingHole = hole;
            canCompleteSwallow = canBeConsumed;
            lastInfluenceTime = Time.time;
            ActivatePhysics();
            ApplyHoleForces(hole, canBeConsumed);
        }

        public bool CanFitThrough(float holeRadius, float clearance)
        {
            return holeRadius >= RequiredPassThroughRadius + clearance;
        }

        public void ResetForReuse()
        {
            isConsumed = false;
            passThroughEnabled = false;
            influencingHole = null;
            canCompleteSwallow = false;
            transform.localScale = initialScale == Vector3.zero ? Vector3.one : initialScale;
            RestoreColliderTriggers();

            if (body != null)
            {
                SetLinearVelocity(Vector3.zero);
                body.angularVelocity = Vector3.zero;
                ConfigureBody();
            }

            RecalculateMetrics();
        }

        private void ApplyHoleForces(HoleBase hole, bool canBeConsumed)
        {
            if (body == null)
            {
                return;
            }

            Vector3 center = body.worldCenterOfMass;
            Vector3 holeCenter = hole.transform.position;
            Vector3 toHole = holeCenter - center;
            toHole.y = 0f;

            if (toHole.sqrMagnitude < 0.0001f)
            {
                toHole = -transform.forward;
            }

            Vector3 inwardDirection = toHole.normalized;
            float distance = toHole.magnitude;
            float influenceRange = hole.Radius + Size;
            float influence = Mathf.Clamp01(1f - distance / Mathf.Max(0.001f, influenceRange));
            float consumeMultiplier = canBeConsumed ? 1f : oversizedInfluenceMultiplier;

            Vector3 edgePoint = center + inwardDirection * Size;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, inwardDirection).normalized;
            if (rollAxis.sqrMagnitude < 0.0001f)
            {
                rollAxis = Vector3.right;
            }

            body.AddForce(inwardDirection * (inwardAcceleration * consumeMultiplier * influence), ForceMode.Acceleration);
            body.AddForceAtPosition(Vector3.down * (edgeDownAcceleration * consumeMultiplier * influence), edgePoint, ForceMode.Acceleration);
            body.AddTorque(rollAxis * (tumbleTorque * consumeMultiplier * influence), ForceMode.Acceleration);
            ClampFallSpeed();
        }

        private bool IsReadyToFallThrough(HoleBase hole)
        {
            Vector3 toHole = SwallowCenter - hole.transform.position;
            toHole.y = 0f;
            float requiredRadius = RequiredPassThroughRadius * passThroughSizeFactor;
            return toHole.magnitude + requiredRadius <= hole.Radius;
        }

        private bool HasFallenThrough(HoleBase hole)
        {
            return SwallowCenter.y <= hole.transform.position.y - consumeDepth;
        }

        private void CompleteSwallow(HoleBase hole)
        {
            isConsumed = true;
            passThroughEnabled = false;
            influencingHole = null;
            canCompleteSwallow = false;
            hole.NotifySwallowComplete(this);
            gameObject.SetActive(false);
        }

        private void ActivatePhysics()
        {
            if (body == null)
            {
                CacheComponents();
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.WakeUp();
        }

        private void ConfigureBody()
        {
            body.isKinematic = !activatePhysicsOnStart;
            body.useGravity = activatePhysicsOnStart;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = activatePhysicsOnStart
                ? CollisionDetectionMode.ContinuousDynamic
                : CollisionDetectionMode.ContinuousSpeculative;
        }

        private void SetPassThrough(bool enabled)
        {
            passThroughEnabled = enabled;
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].isTrigger = enabled;
                }
            }
        }

        private void RestoreColliderTriggers()
        {
            if (cachedColliders == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    bool originalState = initialTriggerStates != null && i < initialTriggerStates.Length && initialTriggerStates[i];
                    cachedColliders[i].isTrigger = originalState;
                    cachedColliders[i].enabled = true;
                }
            }
        }

        private void CacheComponents()
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            initialTriggerStates = new bool[cachedColliders.Length];
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                initialTriggerStates[i] = cachedColliders[i] != null && cachedColliders[i].isTrigger;
            }

            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }
        }

        private void RecalculateMetrics()
        {
            if (size <= 0f)
            {
                size = EstimateSize();
            }

            cachedVolume = EstimateVolume();
        }

        private float EstimateSize()
        {
            return MathExt.HorizontalRadius(CalculateWorldBounds());
        }

        private float CalculateRequiredPassThroughRadius()
        {
            Bounds bounds = CalculateWorldBounds();
            Vector3 extents = bounds.extents;
            return Mathf.Sqrt(extents.x * extents.x + extents.z * extents.z);
        }

        private float EstimateVolume()
        {
            return MathExt.BoundsVolume(CalculateWorldBounds());
        }

        private Bounds CalculateWorldBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return bounds;
            }

            Collider[] colliders = cachedColliders != null && cachedColliders.Length > 0
                ? cachedColliders
                : GetComponentsInChildren<Collider>();

            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }

                return bounds;
            }

            return new Bounds(transform.position, Vector3.one);
        }

        private void ClampFallSpeed()
        {
            Vector3 velocity = GetLinearVelocity();
            if (velocity.magnitude > maxFallSpeed)
            {
                SetLinearVelocity(velocity.normalized * maxFallSpeed);
            }
        }

        private Vector3 GetLinearVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        private void SetLinearVelocity(Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
    }
}
