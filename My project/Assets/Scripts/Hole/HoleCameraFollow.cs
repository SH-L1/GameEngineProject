using UnityEngine;
using VoidEater.Objects;

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
        [SerializeField, Min(1f)] private float minOrthographicSize = 8f;
        [SerializeField, Min(1f)] private float maxOrthographicSize = 28f;
        [SerializeField, Min(0f)] private float swallowPulseSize = 1.1f;
        [SerializeField, Min(0.01f)] private float swallowPulseDuration = 0.22f;

        private float pulseTimer;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        private void OnEnable()
        {
            SubscribeTarget();
        }

        private void OnDisable()
        {
            UnsubscribeTarget();
        }

        private void OnValidate()
        {
            if (maxOrthographicSize < minOrthographicSize)
            {
                maxOrthographicSize = minOrthographicSize;
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
                float pulse = CalculatePulse();
                float desiredSize = baseOrthographicSize + target.Radius * sizePerRadius + pulse;
                desiredSize = Mathf.Clamp(desiredSize, minOrthographicSize, maxOrthographicSize);
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, desiredSize, t);
            }
        }

        private float CalculatePulse()
        {
            if (pulseTimer <= 0f)
            {
                return 0f;
            }

            pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime);
            float normalized = pulseTimer / swallowPulseDuration;
            return Mathf.Sin(normalized * Mathf.PI) * swallowPulseSize;
        }

        private void HandleSwallowed(Swallowable _)
        {
            pulseTimer = swallowPulseDuration;
        }

        private void SubscribeTarget()
        {
            if (target != null)
            {
                target.Swallowed += HandleSwallowed;
            }
        }

        private void UnsubscribeTarget()
        {
            if (target != null)
            {
                target.Swallowed -= HandleSwallowed;
            }
        }
    }
}
