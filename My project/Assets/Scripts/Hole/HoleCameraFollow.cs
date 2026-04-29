using UnityEngine;

namespace VoidEater.Hole
{
    public sealed class HoleCameraFollow : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -14f);
        [SerializeField, Min(0.01f)] private float followSharpness = 8f;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(1f)] private float baseOrthographicSize = 10f;
        [SerializeField, Min(0f)] private float sizePerRadius = 1.25f;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.transform.position + offset;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.LookAt(target.transform.position);

            if (targetCamera != null && targetCamera.orthographic)
            {
                float desiredSize = baseOrthographicSize + target.Radius * sizePerRadius;
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, desiredSize, t);
            }
        }
    }
}
