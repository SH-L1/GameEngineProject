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
        [SerializeField, Min(0.05f)] private float consumeDepth = 2.5f;
        [SerializeField, Min(0.05f)] private float influenceMemory = 0.15f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 9f;

        [Header("Hole Visibility")]
        [SerializeField, Range(0.05f, 1f)] private float occlusionRevealAlpha = 0.18f;
        [SerializeField] private Color occlusionOutlineColor = Color.white;
        [SerializeField, Min(0.001f)] private float occlusionOutlineWidth = 0.04f;
        [SerializeField, Min(0.01f)] private float occlusionMemory = 0.08f;

        private Collider[] cachedColliders;
        private bool[] initialTriggerStates;
        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;
        private Material[][] revealMaterials;
        private LineRenderer occlusionOutline;
        private Rigidbody body;
        private Vector3 initialScale;
        private float cachedVolume = -1f;
        private HoleBase influencingHole;
        private bool canCompleteSwallow;
        private bool passThroughEnabled;
        private bool isConsumed;
        private float lastInfluenceTime;
        private float lastOcclusionTime;
        private bool occlusionRevealActive;

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

        private void OnDisable()
        {
            SetOcclusionReveal(false);
        }

        private void OnValidate()
        {
            consumeDepth = Mathf.Max(0.05f, consumeDepth);
            influenceMemory = Mathf.Max(0.05f, influenceMemory);
            occlusionMemory = Mathf.Max(0.01f, occlusionMemory);
        }

        private void FixedUpdate()
        {
            RefreshOcclusionReveal();

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

            ApplyHoleForces(influencingHole);

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
            if (hole == null || isConsumed || !canBeConsumed)
            {
                return;
            }

            influencingHole = hole;
            canCompleteSwallow = true;
            lastInfluenceTime = Time.time;
            ActivatePhysics();
            ApplyHoleForces(hole);
        }

        public void ApplyHoleOcclusion(HoleBase hole)
        {
            if (hole == null || isConsumed || !IsHoleUnderFootprint(hole))
            {
                return;
            }

            lastOcclusionTime = Time.time;
            SetOcclusionReveal(true);
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
            lastOcclusionTime = 0f;
            SetOcclusionReveal(false);
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

        private void ApplyHoleForces(HoleBase hole)
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

            Vector3 edgePoint = center + inwardDirection * Size;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, inwardDirection).normalized;
            if (rollAxis.sqrMagnitude < 0.0001f)
            {
                rollAxis = Vector3.right;
            }

            body.AddForce(inwardDirection * (inwardAcceleration * influence), ForceMode.Acceleration);
            body.AddForceAtPosition(Vector3.down * (edgeDownAcceleration * influence), edgePoint, ForceMode.Acceleration);
            body.AddTorque(rollAxis * (tumbleTorque * influence), ForceMode.Acceleration);
            ClampFallSpeed();
        }

        private bool IsReadyToFallThrough(HoleBase hole)
        {
            Vector3 toHole = SwallowCenter - hole.transform.position;
            toHole.y = 0f;
            return toHole.magnitude + RequiredPassThroughRadius <= hole.Radius;
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
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
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

            EnsureVisualCaches();
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
            float requiredRadius = 0f;
            if (TryCalculateColliderPassThroughRadius(out float colliderRadius))
            {
                requiredRadius = Mathf.Max(requiredRadius, colliderRadius);
            }

            if (TryCalculateRendererPassThroughRadius(out float rendererRadius))
            {
                requiredRadius = Mathf.Max(requiredRadius, rendererRadius);
            }

            if (requiredRadius > 0f)
            {
                return requiredRadius;
            }

            return CalculateBoundsPassThroughRadius(CalculateWorldBounds());
        }

        private float EstimateVolume()
        {
            return MathExt.BoundsVolume(CalculateWorldBounds());
        }

        private Bounds CalculateWorldBounds()
        {
            if (TryCalculateColliderBounds(out Bounds colliderBounds))
            {
                return colliderBounds;
            }

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

            return new Bounds(transform.position, Vector3.one);
        }

        private bool IsHoleUnderFootprint(HoleBase hole)
        {
            Bounds bounds = CalculateWorldBounds();
            Vector3 holePosition = hole.transform.position;
            float padding = Mathf.Max(0.05f, hole.Radius);

            bool insideHorizontalFootprint =
                holePosition.x >= bounds.min.x - padding &&
                holePosition.x <= bounds.max.x + padding &&
                holePosition.z >= bounds.min.z - padding &&
                holePosition.z <= bounds.max.z + padding;

            bool belowObjectTop = holePosition.y <= bounds.max.y + padding;
            bool nearObjectBottom = holePosition.y >= bounds.min.y - padding;

            return insideHorizontalFootprint && belowObjectTop && nearObjectBottom;
        }

        private void EnsureVisualCaches()
        {
            if (cachedRenderers == null)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            originalMaterials = new Material[cachedRenderers.Length][];
            revealMaterials = new Material[cachedRenderers.Length][];

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null || renderer is LineRenderer)
                {
                    originalMaterials[i] = System.Array.Empty<Material>();
                    revealMaterials[i] = System.Array.Empty<Material>();
                    continue;
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                originalMaterials[i] = sourceMaterials;
                revealMaterials[i] = new Material[sourceMaterials.Length];
                for (int j = 0; j < sourceMaterials.Length; j++)
                {
                    revealMaterials[i][j] = CreateRevealMaterial(sourceMaterials[j]);
                }
            }
        }

        private Material CreateRevealMaterial(Material source)
        {
            Shader shader = FindRevealShader();

            Material material = shader != null || source == null
                ? new Material(shader)
                : new Material(source);

            Color baseColor = source != null && source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor")
                : source != null && source.HasProperty("_Color")
                    ? source.GetColor("_Color")
                : Color.white;
            baseColor.a = occlusionRevealAlpha;

            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            ConfigureTransparentMaterial(material);
            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static Shader FindRevealShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Hidden/Internal-Colored");
        }

        private void RefreshOcclusionReveal()
        {
            if (!occlusionRevealActive)
            {
                return;
            }

            if (Time.time - lastOcclusionTime > occlusionMemory)
            {
                SetOcclusionReveal(false);
            }
            else
            {
                UpdateOcclusionOutline();
            }
        }

        private void SetOcclusionReveal(bool active)
        {
            if (occlusionRevealActive == active)
            {
                if (active)
                {
                    UpdateOcclusionOutline();
                }

                return;
            }

            if (cachedRenderers == null || originalMaterials == null || revealMaterials == null)
            {
                EnsureVisualCaches();
            }

            occlusionRevealActive = active;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null || renderer is LineRenderer)
                {
                    continue;
                }

                Material[] materials = active ? revealMaterials[i] : originalMaterials[i];
                if (materials != null && materials.Length > 0)
                {
                    renderer.sharedMaterials = materials;
                }
            }

            EnsureOcclusionOutline();
            occlusionOutline.enabled = active;
            if (active)
            {
                UpdateOcclusionOutline();
            }
        }

        private void EnsureOcclusionOutline()
        {
            if (occlusionOutline != null)
            {
                return;
            }

            Transform child = transform.Find("HoleOcclusionOutline");
            if (child == null)
            {
                child = new GameObject("HoleOcclusionOutline").transform;
                child.SetParent(transform, false);
            }

            occlusionOutline = child.GetComponent<LineRenderer>();
            if (occlusionOutline == null)
            {
                occlusionOutline = child.gameObject.AddComponent<LineRenderer>();
            }

            Material material = new Material(FindRevealShader());
            material.color = occlusionOutlineColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", occlusionOutlineColor);
            }

            occlusionOutline.sharedMaterial = material;
            occlusionOutline.useWorldSpace = true;
            occlusionOutline.loop = false;
            occlusionOutline.alignment = LineAlignment.View;
            occlusionOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            occlusionOutline.receiveShadows = false;
            occlusionOutline.enabled = false;
        }

        private void UpdateOcclusionOutline()
        {
            EnsureOcclusionOutline();
            Bounds bounds = CalculateWorldBounds();
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            Vector3 p000 = new Vector3(min.x, min.y, min.z);
            Vector3 p100 = new Vector3(max.x, min.y, min.z);
            Vector3 p110 = new Vector3(max.x, min.y, max.z);
            Vector3 p010 = new Vector3(min.x, min.y, max.z);
            Vector3 p001 = new Vector3(min.x, max.y, min.z);
            Vector3 p101 = new Vector3(max.x, max.y, min.z);
            Vector3 p111 = new Vector3(max.x, max.y, max.z);
            Vector3 p011 = new Vector3(min.x, max.y, max.z);

            occlusionOutline.positionCount = 16;
            occlusionOutline.startWidth = occlusionOutlineWidth;
            occlusionOutline.endWidth = occlusionOutlineWidth;
            occlusionOutline.startColor = occlusionOutlineColor;
            occlusionOutline.endColor = occlusionOutlineColor;
            occlusionOutline.SetPosition(0, p000);
            occlusionOutline.SetPosition(1, p100);
            occlusionOutline.SetPosition(2, p110);
            occlusionOutline.SetPosition(3, p010);
            occlusionOutline.SetPosition(4, p000);
            occlusionOutline.SetPosition(5, p001);
            occlusionOutline.SetPosition(6, p101);
            occlusionOutline.SetPosition(7, p100);
            occlusionOutline.SetPosition(8, p101);
            occlusionOutline.SetPosition(9, p111);
            occlusionOutline.SetPosition(10, p110);
            occlusionOutline.SetPosition(11, p111);
            occlusionOutline.SetPosition(12, p011);
            occlusionOutline.SetPosition(13, p010);
            occlusionOutline.SetPosition(14, p011);
            occlusionOutline.SetPosition(15, p001);
        }

        private bool TryCalculateColliderBounds(out Bounds bounds)
        {
            Collider[] colliders = cachedColliders != null && cachedColliders.Length > 0
                ? cachedColliders
                : GetComponentsInChildren<Collider>();

            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = candidate.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(candidate.bounds);
            }

            return hasBounds;
        }

        private bool TryCalculateColliderPassThroughRadius(out float radius)
        {
            Collider[] colliders = cachedColliders != null && cachedColliders.Length > 0
                ? cachedColliders
                : GetComponentsInChildren<Collider>();

            radius = 0f;
            if (!TryCalculateColliderBounds(out Bounds combinedBounds))
            {
                return false;
            }

            Vector3 footprintCenter = combinedBounds.center;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled)
                {
                    continue;
                }

                radius = Mathf.Max(radius, CalculateColliderPassThroughRadius(candidate, footprintCenter));
            }

            return radius > 0f;
        }

        private static float CalculateColliderPassThroughRadius(Collider collider, Vector3 footprintCenter)
        {
            if (collider is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                Vector3 scale = sphere.transform.lossyScale;
                float horizontalRadius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                return HorizontalDistance(footprintCenter, center) + horizontalRadius;
            }

            if (collider is CapsuleCollider capsule)
            {
                return CalculateCapsulePassThroughRadius(capsule, footprintCenter);
            }

            if (collider is BoxCollider box)
            {
                return CalculateBoxPassThroughRadius(box, footprintCenter);
            }

            Bounds bounds = collider.bounds;
            return HorizontalDistance(footprintCenter, bounds.center) + CalculateBoundsPassThroughRadius(bounds);
        }

        private static float CalculateBoxPassThroughRadius(BoxCollider box, Vector3 footprintCenter)
        {
            Vector3 halfSize = box.size * 0.5f;
            float maxRadius = 0f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localPoint = box.center + Vector3.Scale(halfSize, new Vector3(x, y, z));
                        Vector3 worldPoint = box.transform.TransformPoint(localPoint);
                        maxRadius = Mathf.Max(maxRadius, HorizontalDistance(footprintCenter, worldPoint));
                    }
                }
            }

            return maxRadius;
        }

        private static float CalculateCapsulePassThroughRadius(CapsuleCollider capsule, Vector3 footprintCenter)
        {
            Transform capsuleTransform = capsule.transform;
            Vector3 scale = capsuleTransform.lossyScale;
            Vector3 center = capsuleTransform.TransformPoint(capsule.center);

            int axis = capsule.direction;
            float axisScale = Mathf.Abs(GetAxis(scale, axis));
            float radiusScale = Mathf.Max(
                Mathf.Abs(GetAxis(scale, (axis + 1) % 3)),
                Mathf.Abs(GetAxis(scale, (axis + 2) % 3)));

            float worldRadius = capsule.radius * radiusScale;
            float cylinderHalfLength = Mathf.Max(0f, capsule.height * axisScale * 0.5f - worldRadius);
            Vector3 axisDirection = GetWorldAxis(capsuleTransform, axis);
            Vector3 endpointA = center + axisDirection * cylinderHalfLength;
            Vector3 endpointB = center - axisDirection * cylinderHalfLength;

            return Mathf.Max(
                HorizontalDistance(footprintCenter, endpointA) + worldRadius,
                HorizontalDistance(footprintCenter, endpointB) + worldRadius);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private bool TryCalculateRendererPassThroughRadius(out float radius)
        {
            Renderer[] renderers = cachedRenderers != null && cachedRenderers.Length > 0
                ? cachedRenderers
                : GetComponentsInChildren<Renderer>();

            radius = 0f;
            if (!TryCalculateRendererBounds(out Bounds combinedBounds))
            {
                return false;
            }

            Vector3 footprintCenter = combinedBounds.center;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer is LineRenderer)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                radius = Mathf.Max(radius, HorizontalDistance(footprintCenter, bounds.center) + CalculateBoundsPassThroughRadius(bounds));
            }

            return radius > 0f;
        }

        private bool TryCalculateRendererBounds(out Bounds bounds)
        {
            Renderer[] renderers = cachedRenderers != null && cachedRenderers.Length > 0
                ? cachedRenderers
                : GetComponentsInChildren<Renderer>();

            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer is LineRenderer)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static float CalculateBoundsPassThroughRadius(Bounds bounds)
        {
            Vector3 extents = bounds.extents;
            return Mathf.Sqrt(extents.x * extents.x + extents.z * extents.z);
        }

        private static float GetAxis(Vector3 vector, int axis)
        {
            return axis == 0 ? vector.x : axis == 1 ? vector.y : vector.z;
        }

        private static Vector3 GetWorldAxis(Transform source, int axis)
        {
            if (axis == 0)
            {
                return source.right;
            }

            return axis == 1 ? source.up : source.forward;
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
