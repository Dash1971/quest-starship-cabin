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
        private static readonly int TwinkleId = Shader.PropertyToID("_Twinkle");
        private static readonly int MeteorsId = Shader.PropertyToID("_Meteors");
        private static readonly int GraceStartId = Shader.PropertyToID("_GraceStart");
        private static readonly int VistaClockId = Shader.PropertyToID("_VistaClock");
        private static readonly int DensityId = Shader.PropertyToID("_Density");

        [SerializeField] private float nebulaBlendSeconds = 3.5f;

        private Renderer surfaceRenderer;
        private MaterialPropertyBlock block;
        private float nebulaCurrent;
        private float nebulaTarget;

        private void Awake()
        {
            surfaceRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            ResetVistaClock();
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

        public void SetQuietWatchComfort(bool living, bool drifting)
        {
            SetMotion(drifting ? 0.010f : 0f, drifting ? 0.003f : 0f);
            SetFloat(TwinkleId, living ? 0.045f : 0.012f);
            SetFloat(MeteorsId, 0f);
            SetNebula(0f);
            SetFloat(DensityId, 0.78f);
        }

        public void SetAuthoredVistaBackdrop(float density)
        {
            SetMotion(0f, 0f);
            SetFloat(TwinkleId, 0.008f);
            SetFloat(MeteorsId, 0f);
            SetNebula(0f);
            SetFloat(DensityId, Mathf.Clamp(density, 0.2f, 1f));
            ClearGraceNote();
        }

        public void ResetVistaClock()
        {
            SetFloat(VistaClockId, Time.unscaledTime);
            ClearGraceNote();
        }

        public void TriggerFirstQuestionComet()
        {
            SetFloat(GraceStartId, Time.unscaledTime);
        }

        public void ClearGraceNote()
        {
            SetFloat(GraceStartId, -1000f);
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
