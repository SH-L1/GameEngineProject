using UnityEngine;
using VoidEater.Core;
using VoidEater.Hole;

namespace VoidEater.Player
{
    public sealed class AIHole : HoleBase
    {
        [SerializeField, Min(0f)] private float moveSpeed = 13.5f;
        [SerializeField] private Vector2 mapHalfExtents = new Vector2(45f, 45f);
        [SerializeField, Min(0.1f)] private float detectionRadius = 12f;
        [SerializeField, Min(0.1f)] private float decisionInterval = 0.35f;
        [SerializeField, Min(0.1f)] private float wanderTargetDistance = 8f;
        [SerializeField] private HoleGameController gameController;

        private Vector3 desiredDirection;
        private Vector3 wanderTarget;
        private float nextDecisionTime;

        private void Start()
        {
            if (gameController == null)
            {
                gameController = FindFirstObjectByType<HoleGameController>();
            }

            PickWanderTarget();
        }

        public void SetGameController(HoleGameController controller)
        {
            gameController = controller;
        }

        private void FixedUpdate()
        {
            if (gameController != null && !gameController.IsRunning)
            {
                desiredDirection = Vector3.zero;
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + decisionInterval;
                ChooseDirection();
            }

            Vector3 delta = desiredDirection * (moveSpeed * Time.fixedDeltaTime);
            Vector3 nextPosition = Body.position + delta;
            nextPosition.x = Mathf.Clamp(nextPosition.x, -mapHalfExtents.x, mapHalfExtents.x);
            nextPosition.z = Mathf.Clamp(nextPosition.z, -mapHalfExtents.y, mapHalfExtents.y);
            Body.MovePosition(nextPosition);
        }

        private void ChooseDirection()
        {
            HoleBase nearestHole = FindNearestRelevantHole();
            if (nearestHole != null)
            {
                Vector3 toOther = nearestHole.transform.position - transform.position;
                toOther.y = 0f;
                if (toOther.sqrMagnitude > 0.001f)
                {
                    desiredDirection = nearestHole.Radius > Radius ? -toOther.normalized : toOther.normalized;
                    return;
                }
            }

            if (Vector3.Distance(transform.position, wanderTarget) <= 1f)
            {
                PickWanderTarget();
            }

            Vector3 toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
            desiredDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector3.zero;
        }

        private HoleBase FindNearestRelevantHole()
        {
            HoleBase[] holes = FindObjectsByType<HoleBase>(FindObjectsSortMode.None);
            HoleBase nearest = null;
            float nearestDistance = detectionRadius;

            for (int i = 0; i < holes.Length; i++)
            {
                HoleBase candidate = holes[i];
                if (candidate == null || candidate == this)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void PickWanderTarget()
        {
            Vector2 random = Random.insideUnitCircle.normalized * wanderTargetDistance;
            if (random.sqrMagnitude < 0.001f)
            {
                random = Vector2.right * wanderTargetDistance;
            }

            Vector3 target = transform.position + new Vector3(random.x, 0f, random.y);
            target.x = Mathf.Clamp(target.x, -mapHalfExtents.x, mapHalfExtents.x);
            target.z = Mathf.Clamp(target.z, -mapHalfExtents.y, mapHalfExtents.y);
            wanderTarget = target;
        }
    }
}
