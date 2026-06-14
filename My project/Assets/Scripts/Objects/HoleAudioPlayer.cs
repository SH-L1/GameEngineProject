using UnityEngine;
using VoidEater.Objects; // Swallowable 컴포넌트를 찾기 위해 필요합니다.

namespace VoidEater.Hole
{
    // 이 스크립트를 넣으면 소리를 내는 오디오 소스 컴포넌트도 자동으로 함께 추가됩니다.
    [RequireComponent(typeof(AudioSource))]
    public sealed class HoleAudioPlayer : MonoBehaviour
    {
        [SerializeField] private HoleBase target;      // 구멍 스크립트를 연결할 곳
        [SerializeField] private AudioClip eatSound;   // 재생할 소리 파일을 연결할 곳

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            // 게임 시작 시 자동으로 소리가 나거나 반복 재생되지 않도록 설정
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        private void OnEnable()
        {
            if (target != null)
            {
                // 구멍이 오브젝트를 삼켰을 때(Swallowed) 실행할 메서드를 등록합니다.
                target.Swallowed += HandleSwallowed;
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                // 에러 방지를 위해 등록했던 이벤트를 해제합니다.
                target.Swallowed -= HandleSwallowed;
            }
        }

        // 오브젝트를 먹을 때마다 실제로 소리를 내는 부분입니다.
        private void HandleSwallowed(Swallowable swallowedObject)
        {
            if (audioSource != null && eatSound != null)
            {
                // PlayOneShot은 소리가 연속으로 겹쳐서 나도 끊기지 않게 해줍니다.
                audioSource.PlayOneShot(eatSound);
            }
        }
    }
}