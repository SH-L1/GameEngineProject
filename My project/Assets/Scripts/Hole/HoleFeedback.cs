using UnityEngine;
using VoidEater.Objects;

namespace VoidEater.Hole
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class HoleFeedback : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem swallowParticles;
        [SerializeField] private ParticleSystem levelUpParticles;
        [SerializeField] private AudioClip swallowClip;
        [SerializeField] private AudioClip levelUpClip;
        [SerializeField, Range(0f, 1f)] private float swallowVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float levelUpVolume = 0.75f;
        [SerializeField, Min(0f)] private float visualPulseScale = 0.12f;
        [SerializeField, Min(0.01f)] private float visualPulseDuration = 0.16f;

        private Transform visualRoot;
        private Vector3 baseVisualScale = Vector3.one;
        private float pulseTimer;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ConfigureAudioSource();
            EnsureDefaultClips();
            CacheVisualRoot();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            UpdatePulse();
        }

        public void SetTarget(HoleBase nextTarget)
        {
            Unsubscribe();
            target = nextTarget;
            CacheVisualRoot();
            Subscribe();
        }

        private void Subscribe()
        {
            if (target == null)
            {
                return;
            }

            target.Swallowed += HandleSwallowed;
            target.LevelChanged += HandleLevelChanged;
            target.RadiusChanged += HandleRadiusChanged;
        }

        private void Unsubscribe()
        {
            if (target == null)
            {
                return;
            }

            target.Swallowed -= HandleSwallowed;
            target.LevelChanged -= HandleLevelChanged;
            target.RadiusChanged -= HandleRadiusChanged;
        }

        private void HandleSwallowed(Swallowable _)
        {
            Play(swallowClip, swallowVolume);
            PlayParticles(swallowParticles);
            pulseTimer = visualPulseDuration;
        }

        private void HandleLevelChanged(int _)
        {
            Play(levelUpClip, levelUpVolume);
            PlayParticles(levelUpParticles);
            pulseTimer = visualPulseDuration * 1.5f;
        }

        private void HandleRadiusChanged(float _)
        {
            CacheVisualRoot();
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void EnsureDefaultClips()
        {
            if (swallowClip == null)
            {
                swallowClip = CreateToneClip("Swallow Tone", 440f, 0.08f);
            }

            if (levelUpClip == null)
            {
                levelUpClip = CreateToneClip("Level Up Tone", 760f, 0.14f);
            }
        }

        private void CacheVisualRoot()
        {
            visualRoot = target != null ? target.transform.Find("visualRoot") : null;
            if (visualRoot != null)
            {
                baseVisualScale = visualRoot.localScale;
            }
        }

        private void UpdatePulse()
        {
            if (visualRoot == null || pulseTimer <= 0f)
            {
                if (visualRoot != null)
                {
                    visualRoot.localScale = baseVisualScale;
                }

                return;
            }

            pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime);
            float normalized = pulseTimer / visualPulseDuration;
            float pulse = Mathf.Sin(normalized * Mathf.PI) * visualPulseScale;
            visualRoot.localScale = baseVisualScale * (1f + pulse);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, volume);
        }

        private static void PlayParticles(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            particles.Play();
        }

        private static AudioClip CreateToneClip(string name, float frequency, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
