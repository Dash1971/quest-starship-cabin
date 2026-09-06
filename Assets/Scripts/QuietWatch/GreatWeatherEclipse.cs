using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>A bounded, authored orbital arc driven only by the vista clock.</summary>
    public sealed class GreatWeatherEclipse : MonoBehaviour
    {
        public const float OrbitRadius = 76f;
        public const float MoonRadius = 1.8f;
        public const float StartAngle = -75f;
        public const float EndAngle = -12.7f;
        public const float SolarAngularRadius = 0.00465f;
        private static readonly int SphereId = Shader.PropertyToID("_OccultorSphere");
        [SerializeField] private Transform moon;
        [SerializeField] private Renderer[] receivers;
        [SerializeField] private Vector3 center, sun, tangent;
        private MaterialPropertyBlock block;

        public void Configure(Transform body, Renderer[] surfaces, Vector3 planetCenter, Vector3 sunDirection, Vector3 ringNormal)
        {
            moon = body;
            receivers = surfaces;
            center = planetCenter;
            sun = sunDirection.normalized;
            // The tangent lies in the ring plane. This short arc remains above
            // the sheet while the moon's projected shadow reaches the globe.
            tangent = Vector3.Cross(ringNormal.normalized, sun).normalized;
            EvaluateAt(0f);
        }

        public Vector3 PositionAt(float progress)
        {
            var angle = Mathf.Lerp(StartAngle, EndAngle, Mathf.Clamp01(progress)) * Mathf.Deg2Rad;
            return center + OrbitRadius * (sun * Mathf.Cos(angle) + tangent * Mathf.Sin(angle));
        }

        public void EvaluateAt(float progress)
        {
            if (moon == null || receivers == null) return;
            moon.localPosition = PositionAt(progress);
            var p = moon.position;
            var sphere = new Vector4(p.x, p.y, p.z, MoonRadius);
            block ??= new MaterialPropertyBlock();
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;
                receiver.GetPropertyBlock(block);
                block.SetVector(SphereId, sphere);
                receiver.SetPropertyBlock(block);
            }
        }
    }
}
