using System.Collections;
using UnityEngine;
using VoidEater.Hole;
using VoidEater.Utils;

namespace VoidEater.Objects
{
    [DisallowMultipleComponent]
    public sealed class Swallowable : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float size;
        [SerializeField, Min(0f)] private int score = 10;
        [SerializeField, Min(0.01f)] private float swallowDuration = 0.25f;
        [SerializeField] private AnimationCurve swallowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Collider[] cachedColliders;
        private Vector3 initialScale;
        private bool isBeingSwallowed;
        private float cachedVolume = -1f;

        public float Size => size > 0f ? size : EstimateSize();
        public int Score => score;
        public float Volume => cachedVolume > 0f ? cachedVolume : EstimateVolume();
        public bool IsBeingSwallowed => isBeingSwallowed;

        private void Awake()
        {
            cachedColliders = GetComponentsInChildren<Collider>();
            initialScale = transform.localScale;

            if (size <= 0f)
            {
                size = EstimateSize();
            }

            cachedVolume = EstimateVolume();
        }

        public bool BeginSwallow(HoleBase hole)
        {
            if (isBeingSwallowed || hole == null)
            {
                return false;
            }

            isBeingSwallowed = true;
            SetCollidersEnabled(false);
            StartCoroutine(SwallowRoutine(hole));
            return true;
        }

        private IEnumerator SwallowRoutine(HoleBase hole)
        {
            Vector3 startPosition = transform.position;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < swallowDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swallowDuration);
                float eased = swallowCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPosition, hole.transform.position, eased);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
                yield return null;
            }

            transform.position = hole.transform.position;
            transform.localScale = Vector3.zero;
            hole.NotifySwallowComplete(this);
            gameObject.SetActive(false);
        }

        private float EstimateSize()
        {
            return MathExt.HorizontalRadius(CalculateWorldBounds());
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

        private void SetCollidersEnabled(bool enabled)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                cachedColliders[i].enabled = enabled;
            }
        }

        private void OnEnable()
        {
            isBeingSwallowed = false;
            transform.localScale = initialScale == Vector3.zero ? transform.localScale : initialScale;

            if (cachedColliders != null)
            {
                SetCollidersEnabled(true);
            }
        }
    }
}
