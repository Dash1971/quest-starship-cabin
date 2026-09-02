using System.Collections;
using UnityEngine;

namespace StarshipCabin
{
    /// <summary>
    /// Provides a gentle arrival fade and an optional wind-down that darkens
    /// the view and eases the global audio level toward silence.
    /// </summary>
    public class SleepSession : MonoBehaviour
    {
        public float arrivalFadeSeconds = 3.5f;
        public float autoWindDownMinutes;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Renderer overlay;
        private MaterialPropertyBlock block;

        private void Start()
        {
            BuildOverlay();
            StartCoroutine(Arrival());
        }

        private void BuildOverlay()
        {
            var cameraObject = Camera.main;
            if (cameraObject == null) return;

            var go = new GameObject("Sleep Overlay");
            go.transform.SetParent(cameraObject.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            go.transform.localRotation = Quaternion.identity;

            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = BuildQuad();

            var renderer = go.AddComponent<MeshRenderer>();
            var shader = Shader.Find("StarshipCabin/FadeOverlay");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            renderer.sharedMaterial = new Material(shader);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            overlay = renderer;
            block = new MaterialPropertyBlock();
            SetAlpha(1f);
        }

        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "Sleep Overlay Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-1.2f, -1.2f, 0f),
                new Vector3(1.2f, -1.2f, 0f),
                new Vector3(1.2f, 1.2f, 0f),
                new Vector3(-1.2f, 1.2f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void SetAlpha(float alpha)
        {
            if (overlay == null) return;
            overlay.GetPropertyBlock(block);
            block.SetColor(ColorId, new Color(0f, 0f, 0f, alpha));
            overlay.SetPropertyBlock(block);
            overlay.enabled = alpha > 0.002f;
        }

        private IEnumerator Arrival()
        {
            yield return Fade(1f, 0f, arrivalFadeSeconds);
            if (autoWindDownMinutes > 0f)
            {
                yield return WindDown(autoWindDownMinutes);
            }
        }

        public void BeginWindDown(float minutes)
        {
            StartCoroutine(WindDown(minutes));
        }

        private IEnumerator WindDown(float minutes)
        {
            var duration = Mathf.Max(1f, minutes * 60f);
            var elapsed = 0f;
            var initialVolume = AudioListener.volume;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                SetAlpha(progress * 0.92f);
                AudioListener.volume = Mathf.Lerp(initialVolume, 0f, progress);
                yield return null;
            }
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            var elapsed = 0f;
            seconds = Mathf.Max(0.01f, seconds);
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }

            SetAlpha(to);
        }
    }
}
