using UnityEngine;

namespace VoidEater.Hole
{
    [ExecuteAlways]
    public sealed class HoleProgressRing : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField] private LineRenderer progressRing;
        [SerializeField] private Material progressMaterial;
        [SerializeField, Min(12)] private int segments = 96;
        [SerializeField, Min(0f)] private float progressRadiusOffset = 0.08f;
        [SerializeField, Min(0.001f)] private float progressWidth = 0.09f;
        [SerializeField] private float ringHeight = 1f;

        private float progress01;

        private void Awake()
        {
            EnsureLineRenderers();
            Redraw();
        }

        private void OnEnable()
        {
            Subscribe();
            Redraw();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            segments = Mathf.Max(12, segments);
            EnsureLineRenderers();
            Redraw();
        }

        public void SetTarget(HoleBase nextTarget)
        {
            Unsubscribe();
            target = nextTarget;
            Subscribe();
            Redraw();
        }

        private void Subscribe()
        {
            if (target == null)
            {
                return;
            }

            target.RadiusChanged += HandleRadiusChanged;
            target.GrowthProgressChanged += HandleProgressChanged;
            progress01 = target.GrowthProgress01;
        }

        private void Unsubscribe()
        {
            if (target == null)
            {
                return;
            }

            target.RadiusChanged -= HandleRadiusChanged;
            target.GrowthProgressChanged -= HandleProgressChanged;
        }

        private void HandleRadiusChanged(float _)
        {
            Redraw();
        }

        private void HandleProgressChanged(float nextProgress)
        {
            progress01 = nextProgress;
            Redraw();
        }

        private void EnsureLineRenderers()
        {
            RemoveLegacyBorderRing();
            progressRing = EnsureLineRenderer(progressRing, "GrowthGaugeRing", false);
        }

        private LineRenderer EnsureLineRenderer(LineRenderer renderer, string childName, bool loop)
        {
            if (renderer == null)
            {
                Transform child = transform.Find(childName);
                if (child == null)
                {
                    child = new GameObject(childName).transform;
                    child.SetParent(transform, false);
                }

                renderer = child.GetComponent<LineRenderer>();
                if (renderer == null)
                {
                    renderer = child.gameObject.AddComponent<LineRenderer>();
                }
            }

            renderer.useWorldSpace = false;
            renderer.loop = loop;
            renderer.alignment = LineAlignment.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.textureMode = LineTextureMode.Stretch;
            return renderer;
        }

        private void Redraw()
        {
            if (progressRing == null)
            {
                return;
            }

            float baseRadius = target != null ? target.Radius : 0.5f;
            DrawCircle(progressRing, baseRadius + progressRadiusOffset, progress01, progressWidth);
        }

        private void DrawCircle(LineRenderer renderer, float radius, float fill, float width)
        {
            fill = Mathf.Clamp01(fill);
            int pointCount = renderer.loop ? segments : Mathf.Max(2, Mathf.CeilToInt(segments * fill) + 1);
            renderer.positionCount = pointCount;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.sharedMaterial = progressMaterial;

            float angleRange = Mathf.PI * 2f * fill;
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0f : i / (float)(pointCount - 1);
                float angle = renderer.loop ? Mathf.PI * 2f * i / segments : angleRange * t;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, ringHeight, Mathf.Sin(angle) * radius);
                renderer.SetPosition(i, position);
            }
        }

        private void RemoveLegacyBorderRing()
        {
            Transform legacyBorder = transform.Find("BorderRing");
            if (legacyBorder == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(legacyBorder.gameObject);
            }
            else
            {
                DestroyImmediate(legacyBorder.gameObject);
            }
        }
    }
}
