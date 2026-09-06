using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    // Additional surfaces consume the director's deterministic clock; no global
    // shader time and no separate Update that can run during pause/capture.
    public sealed class VistaBackdropLayers : MonoBehaviour
    {
        private static readonly int TimeId = Shader.PropertyToID("_ObservationTime");
        private static readonly int DawnId = Shader.PropertyToID("_DawnProgress");
        [SerializeField] private Renderer[] layers;
        private MaterialPropertyBlock block;
        public void Configure(Renderer[] renderers) => layers = renderers;
        public void EvaluateAt(float elapsed, float dawn)
        {
            if (layers == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var layer in layers)
            {
                if (layer == null) continue;
                layer.GetPropertyBlock(block);
                block.SetFloat(TimeId, elapsed);
                block.SetFloat(DawnId, dawn);
                layer.SetPropertyBlock(block);
            }
        }
    }
}
