using VoidEater.Player;
using UnityEngine;

namespace VoidEater.Hole
{
    public sealed class EnemyDirectionIndicator : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField] private Material indicatorMaterial;
        [SerializeField, Min(0.1f)] private float detectionRadius = 16f;
        [SerializeField, Min(0f)] private float radiusOffset = 1.1f;
        [SerializeField, Min(0f)] private float heightOffset = 1.4f;
        [SerializeField, Min(0.1f)] private float indicatorLength = 0.9f;
        [SerializeField, Min(0.1f)] private float indicatorWidth = 0.7f;

        private Transform indicator;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<HoleBase>();
            }

            EnsureIndicator();
            RedrawMesh();
            SetVisible(false);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                EnsureIndicator();
                RedrawMesh();
            }
        }

        public void SetTarget(HoleBase nextTarget)
        {
            target = nextTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                SetVisible(false);
                return;
            }

            HoleBase enemy = FindNearestEnemy();
            if (enemy == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 direction = enemy.transform.position - target.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                SetVisible(false);
                return;
            }

            direction.Normalize();
            EnsureIndicator();
            indicator.position = target.transform.position + direction * (target.Radius + radiusOffset) + Vector3.up * heightOffset;
            indicator.rotation = Quaternion.LookRotation(direction, Vector3.up);
            SetVisible(true);
        }

        private HoleBase FindNearestEnemy()
        {
            AIHole[] enemies = FindObjectsByType<AIHole>(FindObjectsSortMode.None);
            HoleBase nearest = null;
            float nearestDistance = detectionRadius;

            for (int i = 0; i < enemies.Length; i++)
            {
                AIHole enemy = enemies[i];
                if (enemy == null || enemy == target)
                {
                    continue;
                }

                float distance = Vector3.Distance(target.transform.position, enemy.transform.position);
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void EnsureIndicator()
        {
            if (indicator == null)
            {
                Transform existing = transform.Find("EnemyDirectionIndicator");
                indicator = existing != null ? existing : new GameObject("EnemyDirectionIndicator").transform;
                indicator.SetParent(transform, false);
            }

            meshFilter = indicator.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = indicator.gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = indicator.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = indicator.gameObject.AddComponent<MeshRenderer>();
            }

            if (indicatorMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                indicatorMaterial = new Material(shader)
                {
                    color = new Color(1f, 0.15f, 0.05f, 1f)
                };
            }

            meshRenderer.sharedMaterial = indicatorMaterial;
        }

        private void RedrawMesh()
        {
            if (meshFilter == null)
            {
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Enemy Direction Triangle" };
                meshFilter.sharedMesh = mesh;
            }

            float halfWidth = indicatorWidth * 0.5f;
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, indicatorLength),
                new Vector3(-halfWidth, 0f, -indicatorLength * 0.45f),
                new Vector3(halfWidth, 0f, -indicatorLength * 0.45f)
            };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private void SetVisible(bool visible)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }
        }
    }
}
