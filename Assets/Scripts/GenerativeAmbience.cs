using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// A quiet, self-contained ambient drone made from a bass root and soft,
    /// slowly modulated harmonics. It is additive to the existing cabin bed.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GenerativeAmbience : MonoBehaviour
    {
        [Range(0f, 0.5f)] public float volume = 0.16f;
        public float rootHz = 55f;

        private int sampleRate;
        private double phaseRoot;
        private double phaseSecond;
        private double phaseThird;
        private double phaseFourth;
        private double phaseLfo;

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) sampleRate = 48000;

            var source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create(
                "Generative Ambience", sampleRate * 2, 1, sampleRate, true, OnRead);
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.playOnAwake = false;
            source.Play();
        }

        private void OnRead(float[] data)
        {
            var increment = rootHz / sampleRate;
            for (var i = 0; i < data.Length; i++)
            {
                phaseRoot = Wrap(phaseRoot + increment);
                phaseSecond = Wrap(phaseSecond + increment * 2.0);
                phaseThird = Wrap(phaseThird + increment * 3.0);
                phaseFourth = Wrap(phaseFourth + increment * 4.02);
                phaseLfo = Wrap(phaseLfo + 0.05 / sampleRate);

                var tau = System.Math.PI * 2.0;
                var amplitude = 0.6 + 0.4 * System.Math.Sin(phaseLfo * tau);
                var sample = System.Math.Sin(phaseRoot * tau) * 0.5
                           + System.Math.Sin(phaseSecond * tau) * 0.16 * amplitude
                           + System.Math.Sin(phaseThird * tau) * 0.11 * amplitude
                           + System.Math.Sin(phaseFourth * tau) * 0.07 * (1.0 - amplitude);
                data[i] = (float)(sample * volume);
            }
        }

        private static double Wrap(double phase)
        {
            return phase >= 1.0 ? phase - 1.0 : phase;
        }
    }
}
