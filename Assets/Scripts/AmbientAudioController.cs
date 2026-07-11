using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// Procedural ambient audio V2 (Milestone 5).
    ///
    /// The V1 hum was two bare sine waves and read as a test tone. V2 builds a
    /// proper engine bed from three seamless loops:
    ///  - Engine hum: a stack of detuned low partials (27–144 Hz). Detuned
    ///    pairs a quarter-hertz apart create slow beats, and two loop-locked
    ///    amplitude LFOs keep it breathing instead of droning. All partial
    ///    frequencies sit on the loop's frequency grid, so the 12 s buffer
    ///    loops with no seam.
    ///  - Brown noise: leaky-integrated white noise — the deep, sleep-safe
    ///    rumble layer from the roadmap. Crossfaded loop seam.
    ///  - Air circulation: double-lowpassed noise with a slow loop-locked
    ///    swell, like a vent you only notice when it stops.
    /// Panel beeps are kept but much quieter and rarer.
    ///
    /// Public API is unchanged: SetMasterCalmVolume(float).
    /// </summary>
    public class AmbientAudioController : MonoBehaviour
    {
        [Header("Procedural Fallback")]
        [SerializeField] private bool createProceduralLoops = true;

        [Header("Loops")]
        [SerializeField] private AudioSource engineHum;
        [SerializeField] private AudioSource brownNoise;
        [SerializeField] private AudioSource airCirculation;

        [Header("One Shots")]
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip[] panelBeeps;
        [SerializeField] private Vector2 beepIntervalSeconds = new Vector2(45f, 120f);
        [SerializeField, Range(0f, 1f)] private float beepVolume = 0.10f;

        private const int SampleRate = 24000;
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
            PlayLoop(brownNoise);
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
                engineHum.volume = volume * 0.58f;
            }

            if (brownNoise != null)
            {
                brownNoise.volume = volume * 0.30f;
            }

            if (airCirculation != null)
            {
                airCirculation.volume = volume * 0.26f;
            }

            beepVolume = volume * 0.16f;
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
                engineHum.clip = CreateEngineClip();
                engineHum.spatialBlend = 0f;
                engineHum.volume = 0.58f;
            }

            if (brownNoise == null)
            {
                brownNoise = gameObject.AddComponent<AudioSource>();
                brownNoise.clip = CreateBrownClip();
                brownNoise.spatialBlend = 0f;
                brownNoise.volume = 0.30f;
            }

            if (airCirculation == null)
            {
                airCirculation = gameObject.AddComponent<AudioSource>();
                airCirculation.clip = CreateAirClip();
                airCirculation.spatialBlend = 0f;
                airCirculation.volume = 0.26f;
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
                oneShotSource.spatialBlend = 0.45f;
                oneShotSource.volume = 0.35f;
            }

            if (panelBeeps == null || panelBeeps.Length == 0)
            {
                panelBeeps = new[]
                {
                    CreateBeepClip("Soft Panel Beep A", 740f, 0.11f),
                    CreateBeepClip("Soft Panel Beep B", 520f, 0.16f)
                };
            }

            beepVolume = 0.10f;
        }

        /// <summary>
        /// 12 s seamless engine loop. Every partial frequency is an integer
        /// multiple of 1/12 Hz, so each completes whole cycles per loop; the
        /// LFOs run at 2 and 3 cycles per loop for the same reason.
        /// </summary>
        private static AudioClip CreateEngineClip()
        {
            const float loopSeconds = 12f;
            var samples = new float[(int)(SampleRate * loopSeconds)];

            // (frequency Hz, amplitude) — detuned pairs 0.25 Hz apart beat slowly.
            var partials = new[]
            {
                (27.0f, 0.24f),
                (36.0f, 0.50f),
                (36.25f, 0.30f),
                (54.0f, 0.16f),
                (72.0f, 0.20f),
                (72.25f, 0.12f),
                (108.0f, 0.07f),
                (144.25f, 0.04f)
            };

            const float lfoA = 2f / loopSeconds; // 2 cycles per loop
            const float lfoB = 3f / loopSeconds; // 3 cycles per loop

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)SampleRate;
                var value = 0f;

                foreach (var (frequency, amplitude) in partials)
                {
                    value += Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude;
                }

                var breathe = (0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * lfoA * t))
                            * (0.95f + 0.05f * Mathf.Sin(2f * Mathf.PI * lfoB * t + 1.7f));
                samples[i] = value * breathe;
            }

            Normalize(samples, 0.65f);
            return ToClip("Procedural Engine Hum V2", samples);
        }

        /// <summary>10 s brown-noise loop (leaky integrator), crossfaded seam.</summary>
        private static AudioClip CreateBrownClip()
        {
            const float loopSeconds = 10f;
            const float fadeSeconds = 0.5f;
            var raw = GenerateWithTail((int)(SampleRate * loopSeconds), (int)(SampleRate * fadeSeconds), () =>
            {
                var b = 0f;
                return (System.Func<float>)(() =>
                {
                    var white = Random.Range(-1f, 1f);
                    b = b * 0.985f + white * 0.015f;
                    return b;
                });
            });

            Normalize(raw, 0.55f);
            return ToClip("Procedural Brown Noise", raw);
        }

        /// <summary>8 s air-circulation loop: double-lowpassed noise with a slow loop-locked swell.</summary>
        private static AudioClip CreateAirClip()
        {
            const float loopSeconds = 8f;
            const float fadeSeconds = 0.4f;
            const float swell = 2f / loopSeconds; // 2 cycles per loop

            var stage1 = 0f;
            var stage2 = 0f;
            var index = 0;

            var raw = GenerateWithTail((int)(SampleRate * loopSeconds), (int)(SampleRate * fadeSeconds), () =>
            {
                return (System.Func<float>)(() =>
                {
                    var white = Random.Range(-1f, 1f);
                    stage1 = Mathf.Lerp(stage1, white, 0.06f);
                    stage2 = Mathf.Lerp(stage2, stage1, 0.08f);
                    var t = index++ / (float)SampleRate;
                    return stage2 * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * swell * t));
                });
            });

            Normalize(raw, 0.4f);
            return ToClip("Procedural Air Circulation V2", raw);
        }

        /// <summary>
        /// Generates loopLength + fade samples with a stateful generator, then
        /// blends the tail into the head so the loop point is seamless.
        /// </summary>
        private static float[] GenerateWithTail(int loopLength, int fadeLength, System.Func<System.Func<float>> makeGenerator)
        {
            var generate = makeGenerator();
            var extended = new float[loopLength + fadeLength];
            for (var i = 0; i < extended.Length; i++)
            {
                extended[i] = generate();
            }

            var output = new float[loopLength];
            System.Array.Copy(extended, output, loopLength);

            for (var i = 0; i < fadeLength; i++)
            {
                var blend = i / (float)fadeLength;
                output[i] = Mathf.Lerp(extended[loopLength + i], extended[i], blend);
            }

            return output;
        }

        private static void Normalize(float[] samples, float peak)
        {
            var max = 0f;
            foreach (var s in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(s));
            }

            if (max < 0.0001f)
            {
                return;
            }

            var scale = peak / max;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
        }

        private static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateBeepClip(string name, float frequency, float duration)
        {
            var sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                samples[i] = Mathf.Sin(Mathf.PI * 2f * frequency * t) * envelope * 0.34f;
            }

            return ToClip(name, samples);
        }
    }
}
