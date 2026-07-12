using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// Runtime driver for the gas-giant planet.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PlanetSurface : MonoBehaviour
    {
        [Tooltip("Degrees per second about the planet's local Y axis. Keep this tiny.")]
        public float spinDegreesPerSecond = 0.4f;

        [Tooltip("World-space direction the sunlight comes from.")]
        public Vector3 sunDirection = new Vector3(-0.55f, 0.30f, 0.78f);

        private static readonly int SunDirId = Shader.PropertyToID("_SunDir");

        private Renderer planetRenderer;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            planetRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ApplySun();
        }

        private void Update()
        {
            if (spinDegreesPerSecond != 0f)
            {
                transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }

        public void SetSunDirection(Vector3 worldDirection)
        {
            sunDirection = worldDirection;
            ApplySun();
        }

        private void ApplySun()
        {
            if (planetRenderer == null)
            {
                return;
            }

            var d = sunDirection.sqrMagnitude > 1e-6f ? sunDirection.normalized : Vector3.forward;
            planetRenderer.GetPropertyBlock(block);
            block.SetVector(SunDirId, new Vector4(d.x, d.y, d.z, 0f));
            planetRenderer.SetPropertyBlock(block);
        }
    }
}
