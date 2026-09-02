using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// A gentle, endless breath. It can ease either a transform's scale or a
    /// light's intensity with a small sine-smooth pulse.
    /// </summary>
    public class PulseComponent : MonoBehaviour
    {
        public enum Mode
        {
            TransformScale,
            LightIntensity
        }

        public Mode mode = Mode.TransformScale;
        public float breathsPerMinute = 20f;
        public float min = 0.99f;
        public float max = 1.02f;
        public Light targetLight;

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
            if (mode == Mode.LightIntensity && targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }
        }

        private void Update()
        {
            var hertz = Mathf.Max(0.01f, breathsPerMinute) / 60f;
            var phase = Mathf.Sin(Time.time * hertz * Mathf.PI * 2f) * 0.5f + 0.5f;
            var value = Mathf.Lerp(min, max, phase);

            if (mode == Mode.TransformScale)
            {
                transform.localScale = baseScale * value;
            }
            else if (targetLight != null)
            {
                targetLight.intensity = value;
            }
        }
    }
}
