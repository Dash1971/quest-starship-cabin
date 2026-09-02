using System.Collections;
using UnityEngine;

namespace StarshipCabin
{
    [System.Serializable]
    public struct Destination
    {
        public string name;
        public float planetScale;
        public float ringScale;
        public Color bandHi;
        public Color bandMid;
        public Color bandLo;
        public Color storm;
        public Color atmoWarm;
        public Color atmoCool;
        public Vector3 sunDir;
        public float calmVolume;
        public Color roomTint;
        public float roomIntensity;
    }

    /// <summary>
    /// Slowly blends the observation planet between destination moods.
    /// It changes the planet material, planet/ring scale, calm audio volume,
    /// and the mixed room light. The star window remains fixed for comfort.
    /// </summary>
    public class DestinationDirector : MonoBehaviour
    {
        public Renderer planetRenderer;
        public Transform planetTransform;
        public Transform ringTransform;
        public AmbientAudioController audioController;
        public Light roomLight;

        public Destination[] destinations;
        public float dwellSeconds = 150f;
        public float fadeSeconds = 6f;
        public bool autoCycle = true;

        private static readonly int ColHi = Shader.PropertyToID("_ColHi");
        private static readonly int ColMid = Shader.PropertyToID("_ColMid");
        private static readonly int ColLo = Shader.PropertyToID("_ColLo");
        private static readonly int Storm = Shader.PropertyToID("_StormColor");
        private static readonly int AtmoWarm = Shader.PropertyToID("_AtmoWarm");
        private static readonly int AtmoCool = Shader.PropertyToID("_AtmoCool");
        private static readonly int SunDir = Shader.PropertyToID("_SunDir");

        private MaterialPropertyBlock block;
        private Destination current;
        private int index;

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            var planet = GameObject.Find("Planet (Jovian Dawn)");
            if (planet != null)
            {
                if (planetRenderer == null) planetRenderer = planet.GetComponent<Renderer>();
                if (planetTransform == null) planetTransform = planet.transform;
            }

            if (ringTransform == null)
            {
                var ring = GameObject.Find("Planet Ring");
                if (ring != null) ringTransform = ring.transform;
            }

            if (audioController == null)
            {
                audioController = FindAnyObjectByType<AmbientAudioController>();
            }

            if (roomLight == null)
            {
                var lightObject = GameObject.Find("Starlight Fill (Mixed)");
                if (lightObject != null) roomLight = lightObject.GetComponent<Light>();
            }
        }

        private IEnumerator Start()
        {
            if (destinations == null || destinations.Length == 0)
            {
                yield break;
            }

            index = 0;
            ApplyImmediate(destinations[0]);

            if (!autoCycle)
            {
                yield break;
            }

            while (true)
            {
                yield return new WaitForSeconds(dwellSeconds);
                var next = (index + 1) % destinations.Length;
                yield return Blend(destinations[index], destinations[next]);
                index = next;
            }
        }

        public void Next()
        {
            if (destinations == null || destinations.Length == 0) return;
            var next = (index + 1) % destinations.Length;
            StopAllCoroutines();
            StartCoroutine(GoTo(next, autoCycle));
        }

        public void SetDestination(int destinationIndex)
        {
            if (destinations == null || destinations.Length == 0) return;
            StopAllCoroutines();
            var wrappedIndex = ((destinationIndex % destinations.Length) + destinations.Length) % destinations.Length;
            StartCoroutine(GoTo(wrappedIndex, autoCycle));
        }

        private IEnumerator GoTo(int destinationIndex, bool resumeAuto)
        {
            yield return Blend(current, destinations[destinationIndex]);
            index = destinationIndex;

            if (resumeAuto)
            {
                while (true)
                {
                    yield return new WaitForSeconds(dwellSeconds);
                    var next = (index + 1) % destinations.Length;
                    yield return Blend(destinations[index], destinations[next]);
                    index = next;
                }
            }
        }

        private IEnumerator Blend(Destination from, Destination to)
        {
            var elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeSeconds)));
                Apply(from, to, progress);
                yield return null;
            }

            ApplyImmediate(to);
        }

        private void ApplyImmediate(Destination destination)
        {
            Apply(destination, destination, 1f);
            current = destination;
        }

        private void Apply(Destination from, Destination to, float progress)
        {
            if (planetRenderer != null)
            {
                planetRenderer.GetPropertyBlock(block);
                block.SetColor(ColHi, Color.Lerp(from.bandHi, to.bandHi, progress));
                block.SetColor(ColMid, Color.Lerp(from.bandMid, to.bandMid, progress));
                block.SetColor(ColLo, Color.Lerp(from.bandLo, to.bandLo, progress));
                block.SetColor(Storm, Color.Lerp(from.storm, to.storm, progress));
                block.SetColor(AtmoWarm, Color.Lerp(from.atmoWarm, to.atmoWarm, progress));
                block.SetColor(AtmoCool, Color.Lerp(from.atmoCool, to.atmoCool, progress));
                var sunDirection = Vector3.Slerp(from.sunDir.normalized, to.sunDir.normalized, progress);
                block.SetVector(SunDir, new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
                planetRenderer.SetPropertyBlock(block);
            }

            if (planetTransform != null)
            {
                planetTransform.localScale = Vector3.one * Mathf.Lerp(from.planetScale, to.planetScale, progress);
            }

            if (ringTransform != null)
            {
                ringTransform.localScale = Vector3.one * Mathf.Lerp(from.ringScale, to.ringScale, progress);
            }

            if (audioController != null)
            {
                audioController.SetMasterCalmVolume(Mathf.Lerp(from.calmVolume, to.calmVolume, progress));
            }

            if (roomLight != null && (from.roomIntensity > 0.001f || to.roomIntensity > 0.001f))
            {
                roomLight.color = Color.Lerp(from.roomTint, to.roomTint, progress);
                roomLight.intensity = Mathf.Lerp(from.roomIntensity, to.roomIntensity, progress);
            }
        }
    }
}
