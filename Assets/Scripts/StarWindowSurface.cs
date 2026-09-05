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
        private static readonly int ObservationId = Shader.PropertyToID("_ObservationTime");
        private static readonly int GraceAgeId = Shader.PropertyToID("_GraceAge");
        private static readonly int SkyOffsetId = Shader.PropertyToID("_SkyOffset");
        private static readonly int DensityId = Shader.PropertyToID("_Density");

        [SerializeField] private float nebulaBlendSeconds = 3.5f;

        private Renderer surfaceRenderer;
        private MaterialPropertyBlock block;
        private float nebulaCurrent;
        private float nebulaTarget;
        private double observationTime;
        private double offsetX;
        private double offsetY;
        private float targetSpeed, targetDrift, currentSpeed, currentDrift;
        private float graceAge = -1f;
        private bool paused;
        private bool focused = true;

        private void Awake()
        {
            surfaceRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            ResetVistaClock();
        }

        private void Update()
        {
            if (paused || !focused) return;
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            observationTime += dt;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, dt * 0.005f);
            currentDrift = Mathf.MoveTowards(currentDrift, targetDrift, dt * 0.0015f);
            offsetX += currentSpeed * dt * 0.00065;
            offsetY += System.Math.Cos(observationTime * 0.018) * 0.018 * currentDrift * dt * 0.0009;
            WriteClock();
            if (!Mathf.Approximately(nebulaCurrent, nebulaTarget))
            {
                nebulaCurrent = Mathf.MoveTowards(nebulaCurrent, nebulaTarget,
                    nebulaBlendSeconds <= 0f ? 1f : dt / nebulaBlendSeconds);
                SetFloat(NebulaId, nebulaCurrent);
            }
        }

        private void OnApplicationPause(bool value) => paused = value;
        private void OnApplicationFocus(bool value) => focused = value;

        public void PreviewAt(float elapsed, bool drifting, float eventAge)
        {
            observationTime = elapsed;
            offsetX = drifting ? elapsed * 0.010 * 0.00065 : 0;
            offsetY = drifting ? System.Math.Sin(elapsed * 0.018) * 0.003 * 0.0009 : 0;
            graceAge = eventAge;
            WriteClock();
        }

        private void WriteClock()
        {
            SetFloat(ObservationId, (float)observationTime);
            SetFloat(GraceAgeId, graceAge);
            surfaceRenderer.GetPropertyBlock(block);
            block.SetVector(SkyOffsetId, new Vector4((float)offsetX, (float)offsetY, 0f, 0f));
            surfaceRenderer.SetPropertyBlock(block);
        }

        public void SetMotion(float speed, float drift)
        {
            targetSpeed = Mathf.Max(0f, speed);
            targetDrift = Mathf.Max(0f, drift);
            SetFloat(SpeedId, targetSpeed);
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
            observationTime = offsetX = offsetY = 0;
            currentSpeed = currentDrift = 0;
            SetFloat(VistaClockId, Time.unscaledTime);
            WriteClock();
            ClearGraceNote();
        }

        public void SetGraceAge(float age)
        {
            graceAge = age;
            SetFloat(GraceAgeId, age);
        }

        public void TriggerFirstQuestionComet()
        {
            graceAge = 0f;
            SetFloat(GraceStartId, Time.unscaledTime);
            WriteClock();
        }

        public void ClearGraceNote()
        {
            graceAge = -1f;
            SetFloat(GraceStartId, -1000f);
            SetFloat(GraceAgeId, -1f);
        }

        private void SetFloat(int id, float value)
        {
            if (surfaceRenderer == null) surfaceRenderer = GetComponent<Renderer>();
            block ??= new MaterialPropertyBlock();

            surfaceRenderer.GetPropertyBlock(block);
            block.SetFloat(id, value);
            surfaceRenderer.SetPropertyBlock(block);
        }
    }
}
