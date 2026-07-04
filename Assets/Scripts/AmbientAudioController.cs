using UnityEngine;

namespace StarshipCabin
{
    public class AmbientAudioController : MonoBehaviour
    {
        [Header("Procedural Fallback")]
        [SerializeField] private bool createProceduralLoops = true;

        [Header("Loops")]
        [SerializeField] private AudioSource engineHum;
        [SerializeField] private AudioSource airCirculation;

        [Header("One Shots")]
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip[] panelBeeps;
        [SerializeField] private Vector2 beepIntervalSeconds = new Vector2(18f, 55f);
        [SerializeField, Range(0f, 1f)] private float beepVolume = 0.18f;

        private float nextBeepAt;

        private void Awake()
        {
            if (createProceduralLoops)
            {
                EnsureProceduralAudio();
            }

            ScheduleNextBeep();
        }

        private void Start()
        {
            PlayLoop(engineHum);
            PlayLoop(airCirculation);
        }

        private void Update()
        {
            if (panelBeeps == null || panelBeeps.Length == 0 || oneShotSource == null)
            {
                return;
            }

            if (Time.time >= nextBeepAt)
            {
                var clip = panelBeeps[Random.Range(0, panelBeeps.Length)];
                oneShotSource.PlayOneShot(clip, beepVolume);
                ScheduleNextBeep();
            }
        }

        public void SetMasterCalmVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (engineHum != null)
            {
                engineHum.volume = volume * 0.78f;
            }

            if (airCirculation != null)
            {
                airCirculation.volume = volume * 0.50f;
            }

            beepVolume = volume * 0.42f;
        }

        private void PlayLoop(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.loop = true;
            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private void ScheduleNextBeep()
        {
            nextBeepAt = Time.time + Random.Range(beepIntervalSeconds.x, beepIntervalSeconds.y);
        }

        private void EnsureProceduralAudio()
        {
            if (engineHum == null)
            {
                engineHum = gameObject.AddComponent<AudioSource>();
                engineHum.clip = CreateHumClip("Procedural Engine Hum", 42f, 58f, 0.58f);
                engineHum.spatialBlend = 0f;
                engineHum.volume = 0.78f;
            }

            if (airCirculation == null)
            {
                airCirculation = gameObject.AddComponent<AudioSource>();
                airCirculation.clip = CreateAirClip();
                airCirculation.spatialBlend = 0f;
                airCirculation.volume = 0.50f;
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
                oneShotSource.spatialBlend = 0.45f;
                oneShotSource.volume = 0.40f;
            }

            if (panelBeeps == null || panelBeeps.Length == 0)
            {
                panelBeeps = new[]
                {
                    CreateBeepClip("Soft Panel Beep A", 740f, 0.11f),
                    CreateBeepClip("Soft Panel Beep B", 520f, 0.16f)
                };
            }

            beepVolume = 0.42f;
        }

        private static AudioClip CreateHumClip(string name, float lowFrequency, float highFrequency, float amplitude)
        {
            const int sampleRate = 24000;
            const int seconds = 4;
            var samples = new float[sampleRate * seconds];

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var low = Mathf.Sin(Mathf.PI * 2f * lowFrequency * t) * 0.68f;
                var high = Mathf.Sin(Mathf.PI * 2f * highFrequency * t) * 0.32f;
                var pulse = 0.88f + Mathf.Sin(Mathf.PI * 2f * 0.18f * t) * 0.12f;
                samples[i] = (low + high) * amplitude * pulse;
            }

            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAirClip()
        {
            const int sampleRate = 24000;
            const int seconds = 3;
            var samples = new float[sampleRate * seconds];
            var last = 0f;

            for (var i = 0; i < samples.Length; i++)
            {
                var white = Random.Range(-1f, 1f);
                last = Mathf.Lerp(last, white, 0.035f);
                samples[i] = last * 0.20f;
            }

            var clip = AudioClip.Create("Procedural Air Circulation", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateBeepClip(string name, float frequency, float duration)
        {
            const int sampleRate = 24000;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                samples[i] = Mathf.Sin(Mathf.PI * 2f * frequency * t) * envelope * 0.34f;
            }

            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
