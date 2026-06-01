using UnityEngine;

namespace VoidEater.Objects
{
    public class ObjectMover : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 3f;       // 이동 속도
        [SerializeField] private float turnSpeed = 5f;       // 회전 속도
        [SerializeField] private float wanderRadius = 10f;   // 배회 반경 (움직일 범위)
        [SerializeField] private float minWaitTime = 1f;     // 목적지 도착 후 최소 대기 시간
        [SerializeField] private float maxWaitTime = 3f;     // 목적지 도착 후 최대 대기 시간

        private Vector3 targetPosition;
        private float waitTimer;
        private bool isWaiting;
        private Vector3 startPosition;

        private void Start()
        {
            // 처음 스폰된 위치를 기억합니다. 이 위치 기준으로 돌아다닙니다.
            startPosition = transform.position;
            SetNewTargetPosition();
        }

        private void Update()
        {
            if (isWaiting)
            {
                Wait();
            }
            else
            {
                MoveTowardsTarget();
            }
        }

        // 목적지를 향해 이동하고 회전하는 메서드
        private void MoveTowardsTarget()
        {
            // 캐릭터의 높이(Y축)는 유지한 채 X, Z축으로만 이동 방향 계산
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0;

            // 목적지에 거의 도착했는지 확인
            if (direction.magnitude < 0.2f)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
                return;
            }

            // 1. 회전 시키기
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // 2. 앞으로 이동하기
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        // 목적지에 도착했을 때 잠시 멍 때리는(대기) 메서드
        private void Wait()
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                SetNewTargetPosition();
            }
        }

        // 새로운 무작위 목적지 설정
        private void SetNewTargetPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            targetPosition = new Vector3(startPosition.x + randomCircle.x, transform.position.y, startPosition.z + randomCircle.y);
        }

        // 에디터 뷰에서 움직이는 반경을 기즈모(선)로 시각화하여 보여줍니다.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }
    }
}
