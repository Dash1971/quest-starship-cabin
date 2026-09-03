using System;
using System.Collections;
using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>One owner for all comfort-safe blackout transitions.</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer fadeRenderer;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.25f;

        private MaterialPropertyBlock block;

        public bool IsBusy { get; private set; }

        public void Configure(Renderer renderer)
        {
            fadeRenderer = renderer;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            SetAlpha(0f);
            if (fadeRenderer != null)
            {
                fadeRenderer.enabled = false;
            }
        }

        public bool TryBlackout(Action atBlack, Action onComplete = null)
        {
            if (IsBusy)
            {
                return false;
            }

            StartCoroutine(BlackoutRoutine(atBlack, onComplete));
            return true;
        }

        private IEnumerator BlackoutRoutine(Action atBlack, Action onComplete)
        {
            IsBusy = true;
            yield return Fade(0f, 1f);
            atBlack?.Invoke();
            yield return null;
            yield return Fade(1f, 0f);
            IsBusy = false;
            onComplete?.Invoke();
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeRenderer == null)
            {
                yield break;
            }

            fadeRenderer.enabled = true;
            var elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeSeconds)));
                yield return null;
            }

            SetAlpha(to);
            fadeRenderer.enabled = to > 0.001f;
        }

        private void SetAlpha(float alpha)
        {
            if (fadeRenderer == null)
            {
                return;
            }

            fadeRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, new Color(0f, 0f, 0f, alpha));
            fadeRenderer.SetPropertyBlock(block);
        }
    }
}
