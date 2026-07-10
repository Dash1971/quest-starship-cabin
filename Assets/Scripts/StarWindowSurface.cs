using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// Runtime driver for the StarWindow shader quad. Replaces the old
    /// particle-based StarfieldWindow: motion happens inside the shader
    /// (no transform movement, no direction flip) and ambience modes can
    /// blend a slow nebula wash in and out.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class StarWindowSurface : MonoBehaviour
    {
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");
        private static readonly int DriftId = Shader.PropertyToID("_Drift");
        private static readonly int NebulaId = Shader.PropertyToID("_NebulaAmount");

        [SerializeField] private float nebulaBlendSeconds = 3.5f;

        private Renderer surfaceRenderer;
        private MaterialPropertyBlock block;
        private float nebulaCurrent;
        private float nebulaTarget;

        private void Awake()
        {
            surfaceRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (Mathf.Approximately(nebulaCurrent, nebulaTarget))
            {
                return;
            }

            var maxStep = nebulaBlendSeconds <= 0f
                ? 1f
                : Time.deltaTime / nebulaBlendSeconds;
            nebulaCurrent = Mathf.MoveTowards(nebulaCurrent, nebulaTarget, maxStep);
            SetFloat(NebulaId, nebulaCurrent);
        }

        public void SetMotion(float speed, float drift)
        {
            SetFloat(SpeedId, Mathf.Max(0f, speed));
            SetFloat(DriftId, Mathf.Max(0f, drift));
        }

        public void SetNebula(float amount)
        {
            nebulaTarget = Mathf.Clamp01(amount);
        }

        private void SetFloat(int id, float value)
        {
            if (surfaceRenderer == null)
            {
                return;
            }

            surfaceRenderer.GetPropertyBlock(block);
            block.SetFloat(id, value);
            surfaceRenderer.SetPropertyBlock(block);
        }
    }
}
