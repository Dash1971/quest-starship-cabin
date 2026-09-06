using System.Collections.Generic;
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
        [SerializeField] private AudioSource destinationBed;

        [Header("One Shots")]
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip[] panelBeeps;
        [SerializeField] private Vector2 beepIntervalSeconds = new Vector2(45f, 120f);
        [SerializeField, Range(0f, 1f)] private float beepVolume = 0.10f;

        private const int SampleRate = 24000;
        private readonly Dictionary<string, AudioClip> destinationClips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> graceClips = new Dictionary<string, AudioClip>();
        private AudioSource outgoingBed;
        private AudioSource graceSource;
        private bool fadingGrace;
        private float nextBeepAt;
        private float destinationTargetVolume;
        private string currentDestination = string.Empty;

        private void Awake()
        {
            if (createProceduralLoops)
            {
                EnsureProceduralAudio();
                // Synthesis belongs to startup, never a rare event's first frame.
                foreach (var id in new[] { "first-question", "harbour", "blue-morning", "great-weather", "long-formation" })
                {
                    DestinationClip(id);
                    graceClips[id] = CreateGraceClip(id);
                }
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
            if (destinationBed != null)
            {
                destinationBed.volume = Mathf.MoveTowards(
                    destinationBed.volume, destinationTargetVolume, Time.unscaledDeltaTime * 0.10f);
            }

            if (outgoingBed != null && outgoingBed.isPlaying)
            {
                outgoingBed.volume = Mathf.MoveTowards(outgoingBed.volume, 0f, Time.unscaledDeltaTime * 0.6f);
                if (outgoingBed.volume <= 0f) outgoingBed.Stop();
            }
            if (fadingGrace && graceSource != null)
            {
                graceSource.volume = Mathf.MoveTowards(graceSource.volume, 0f, Time.unscaledDeltaTime * 0.64f);
                if (graceSource.volume <= 0f) { graceSource.Stop(); fadingGrace = false; }
            }

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

        public void SetQuietWatchProfile(bool living)
        {
            SetQuietWatchProfile(currentDestination, living);
        }

        /// <summary>
        /// Blends the common cabin bed with one restrained, destination-specific
        /// spatial layer. Clip changes happen behind the vista blackout and fade
        /// back in, so the exterior, light and sound arrive as one place.
        /// </summary>
        public void SetQuietWatchProfile(string vistaId, bool living)
        {
            if (!Application.isPlaying) return;
            EnsureProceduralAudio();
            currentDestination = string.IsNullOrEmpty(vistaId) ? "first-question" : vistaId;
            SetMasterCalmVolume(living ? 0.68f : 0.54f);

            var clip = DestinationClip(currentDestination);
            if (destinationBed != null && destinationBed.clip != clip)
            {
                // Preserve the outgoing source and its position while fading.
                var previous = destinationBed;
                destinationBed = outgoingBed;
                outgoingBed = previous;
                destinationBed.Stop();
                destinationBed.transform.localPosition = DestinationPosition(currentDestination);
                destinationBed.clip = clip;
                destinationBed.volume = 0f;
                if (clip != null) destinationBed.Play();
            }

            destinationTargetVolume = DestinationVolume(currentDestination) * (living ? 1.0f : 0.72f);
            // Quiet mode is uninterrupted shelter. Living mode permits the
            // existing rare, restrained panel acknowledgement.
            beepVolume = living ? 0.055f : 0f;
        }

        public void TriggerQuietWatchGrace(string vistaId)
        {
            if (graceSource == null) return;

            var id = string.IsNullOrEmpty(vistaId) ? currentDestination : vistaId;
            if (!graceClips.TryGetValue(id, out var clip))
            {
                clip = CreateGraceClip(id);
                graceClips[id] = clip;
            }

            if (clip != null)
            {
                fadingGrace = false;
                graceSource.Stop();
                graceSource.clip = clip;
                graceSource.volume = id == "first-question" ? 0.075f : 0.16f;
                graceSource.Play();
            }
        }

        public void CancelQuietWatchGrace() => fadingGrace = true;

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

            if (destinationBed == null) destinationBed = CreateDestinationSource("Destination Spatial Bed A");
            if (outgoingBed == null) outgoingBed = CreateDestinationSource("Destination Spatial Bed B");
            if (graceSource == null)
            {
                graceSource = gameObject.AddComponent<AudioSource>();
                graceSource.playOnAwake = false;
                graceSource.spatialBlend = 0.45f;
                graceSource.dopplerLevel = 0f;
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

        private AudioSource CreateDestinationSource(string name)
        {
            var bedObject = new GameObject(name);
            bedObject.transform.SetParent(transform, false);
            var source = bedObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0.72f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.dopplerLevel = 0f;
            var lowPass = bedObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 1450f;
            lowPass.lowpassResonanceQ = 0.72f;
            return source;
        }

        private AudioClip DestinationClip(string vistaId)
        {
            if (!destinationClips.TryGetValue(vistaId, out var clip))
            {
                clip = CreateDestinationClip(vistaId);
                destinationClips[vistaId] = clip;
            }
            return clip;
        }

        private static float DestinationVolume(string vistaId)
        {
            switch (vistaId)
            {
                case "harbour": return 0.15f;
                case "blue-morning": return 0.075f;
                case "great-weather": return 0.13f;
                case "long-formation": return 0.12f;
                default: return 0.025f;
            }
        }

        private static Vector3 DestinationPosition(string vistaId)
        {
            switch (vistaId)
            {
                case "harbour": return new Vector3(-3.1f, 1.5f, -5.4f);
                case "blue-morning": return new Vector3(0.8f, 2.2f, -6.0f);
                case "great-weather": return new Vector3(2.4f, 0.8f, -5.7f);
                case "long-formation": return new Vector3(2.9f, 1.6f, -5.5f);
                default: return new Vector3(0f, 1.8f, -6.4f);
            }
        }

        /// <summary>
        /// Twelve-second, loop-locked sound identities. They are deliberately
        /// low and abstract: machinery through a hull, atmospheric scale and
        /// distant drives rather than literal effects in vacuum.
        /// </summary>
        private static AudioClip CreateDestinationClip(string vistaId)
        {
            const float seconds = 12f;
            var samples = new float[(int)(SampleRate * seconds)];
            // Stable seed across Mono/IL2CPP and separate capture processes.
            var seed = 17;
            unchecked { foreach (var character in vistaId) seed = seed * 31 + character; }
            var random = new System.Random(seed);
            var filteredNoise = 0f;

            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)SampleRate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.006f);
                float value;
                switch (vistaId)
                {
                    case "harbour":
                        var machinery = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                        value = (Mathf.Sin(2f * Mathf.PI * 31f * t) * 0.38f
                            + Mathf.Sin(2f * Mathf.PI * 62f * t + 0.8f) * 0.16f
                            + Mathf.Sin(2f * Mathf.PI * 93f * t + 0.3f) * 0.10f
                            + Mathf.Sin(2f * Mathf.PI * 124f * t) * 0.045f
                            + filteredNoise * 0.70f) * machinery;
                        break;
                    case "blue-morning":
                        value = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.16f
                            + Mathf.Sin(2f * Mathf.PI * 72f * t + 1.1f) * 0.10f
                            + filteredNoise * (0.48f + 0.12f * Mathf.Sin(2f * Mathf.PI * t / 6f));
                        break;
                    case "great-weather":
                        value = Mathf.Sin(2f * Mathf.PI * 21f * t) * 0.43f
                            + Mathf.Sin(2f * Mathf.PI * 28f * t + 0.6f) * 0.24f
                            + filteredNoise * 0.88f;
                        break;
                    case "long-formation":
                        value = Mathf.Sin(2f * Mathf.PI * 36f * t) * 0.34f
                            + Mathf.Sin(2f * Mathf.PI * 54f * t + 1.6f) * 0.22f
                            + Mathf.Sin(2f * Mathf.PI * 72f * t + 0.2f) * 0.09f
                            + filteredNoise * 0.42f;
                        break;
                    default:
                        value = Mathf.Sin(2f * Mathf.PI * 24f * t) * 0.12f + filteredNoise * 0.16f;
                        break;
                }
                samples[i] = value;
            }

            Normalize(samples, 0.46f);
            return ToClip("Quiet Watch " + vistaId + " ambience", samples);
        }

        private static AudioClip CreateGraceClip(string vistaId)
        {
            var duration = vistaId == "great-weather" ? 4.5f : 2.8f;
            var samples = new float[Mathf.CeilToInt(SampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)SampleRate;
                var progress = Mathf.Clamp01(t / duration);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                float value;
                switch (vistaId)
                {
                    case "harbour":
                        value = Mathf.Sin(2f * Mathf.PI * 246f * t) * 0.22f
                            + Mathf.Sin(2f * Mathf.PI * 369f * t) * 0.10f;
                        break;
                    case "blue-morning":
                        value = Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.24f
                            + Mathf.Sin(2f * Mathf.PI * 144f * t) * 0.12f;
                        break;
                    case "great-weather":
                        value = Mathf.Sin(2f * Mathf.PI * (24f + progress * 7f) * t) * 0.42f;
                        break;
                    case "long-formation":
                        value = Mathf.Sin(2f * Mathf.PI * 164f * t) * 0.18f
                            + Mathf.Sin(2f * Mathf.PI * 219f * t) * 0.10f;
                        break;
                    default:
                        value = Mathf.Sin(2f * Mathf.PI * 310f * t) * 0.16f;
                        break;
                }
                samples[i] = value * envelope;
            }
            return ToClip("Quiet Watch " + vistaId + " grace", samples);
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
