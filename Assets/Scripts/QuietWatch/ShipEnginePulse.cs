using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Low-cost authored drive glow for formation vessels.</summary>
    public sealed class ShipEnginePulse : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform[] glows;
        [SerializeField] private Color driveColor = new Color(0.12f, 0.68f, 1.0f);
        [SerializeField] private float phase;

        private Vector3[] baseScales;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float activity = 0.68f;

        public void Configure(Transform[] driveGlows, float phaseOffset)
        {
            glows = driveGlows;
            phase = phaseOffset;
            Cache();
        }

        public void SetActivity(float value)
        {
            activity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            Cache();
        }

        private void Cache()
        {
            glows ??= System.Array.Empty<Transform>();
            baseScales = new Vector3[glows.Length];
            renderers = new Renderer[glows.Length];
            for (var i = 0; i < glows.Length; i++)
            {
                if (glows[i] == null) continue;
                baseScales[i] = glows[i].localScale;
                renderers[i] = glows[i].GetComponent<Renderer>();
            }
            block ??= new MaterialPropertyBlock();
        }

        private void Update()
        {
            var slow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.13f + phase);
            var fine = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.71f + phase * 1.7f);
            var power = Mathf.Lerp(0.70f, 1.0f, slow * 0.72f + fine * 0.28f) * activity;
            for (var i = 0; i < glows.Length; i++)
            {
                if (glows[i] == null) continue;
                glows[i].localScale = baseScales[i] * Mathf.Lerp(0.88f, 1.16f, power);
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(block);
                block.SetColor(EmissionColorId, driveColor * Mathf.Lerp(3.4f, 6.2f, power));
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
