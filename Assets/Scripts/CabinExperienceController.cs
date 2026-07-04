using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace StarshipCabin
{
    public enum CabinMode
    {
        Drift,
        Orbit,
        Nebula
    }

    public class CabinExperienceController : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private CabinMode mode = CabinMode.Drift;
        [SerializeField] private StarfieldWindow starfieldWindow;
        [SerializeField] private AmbientAudioController audioController;

        [Header("Session")]
        [SerializeField] private int sessionMinutes = 20;
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField] private float fadeSeconds = 4f;
        [SerializeField] private UnityEvent onSessionEnded;

        private Coroutine sessionRoutine;

        private void Start()
        {
            ApplyMode(mode);
            StartSession(sessionMinutes);
        }

        public void SetModeDrift()
        {
            SetMode(CabinMode.Drift);
        }

        public void SetModeOrbit()
        {
            SetMode(CabinMode.Orbit);
        }

        public void SetModeNebula()
        {
            SetMode(CabinMode.Nebula);
        }

        public void SetMode(CabinMode nextMode)
        {
            mode = nextMode;
            ApplyMode(mode);
        }

        public void StartSession(int minutes)
        {
            sessionMinutes = Mathf.Max(1, minutes);

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
            }

            sessionRoutine = StartCoroutine(SessionTimer());
        }

        private void ApplyMode(CabinMode nextMode)
        {
            if (starfieldWindow == null)
            {
                return;
            }

            switch (nextMode)
            {
                case CabinMode.Drift:
                    starfieldWindow.SetMotion(0.025f, 0.0f);
                    break;
                case CabinMode.Orbit:
                    starfieldWindow.SetMotion(0.045f, 0.012f);
                    break;
                case CabinMode.Nebula:
                    starfieldWindow.SetMotion(0.018f, 0.006f);
                    break;
            }
        }

        private IEnumerator SessionTimer()
        {
            yield return new WaitForSeconds(sessionMinutes * 60f);
            yield return FadeOut();
            onSessionEnded?.Invoke();
        }

        private IEnumerator FadeOut()
        {
            if (fadeCanvas == null)
            {
                yield break;
            }

            var start = fadeCanvas.alpha;
            var elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                fadeCanvas.alpha = Mathf.Lerp(start, 1f, elapsed / fadeSeconds);
                yield return null;
            }

            fadeCanvas.alpha = 1f;
        }
    }
}
