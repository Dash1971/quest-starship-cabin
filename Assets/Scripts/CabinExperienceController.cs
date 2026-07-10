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

    /// <summary>
    /// Ambience mode + session controller.
    /// V2 change: drives the StarWindowSurface shader quad while preserving the
    /// legacy StarfieldWindow path for the existing MVP scene. Audio volume is
    /// applied even when no star window is present.
    /// </summary>
    public class CabinExperienceController : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private CabinMode mode = CabinMode.Drift;
        [SerializeField] private StarWindowSurface starWindow;
        [SerializeField] private StarfieldWindow starfieldWindow;
        [SerializeField] private AmbientAudioController audioController;

        [Header("Session")]
        [SerializeField] private int sessionMinutes = 20;
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField] private float fadeSeconds = 4f;
        [SerializeField] private UnityEvent onSessionEnded;

        private Coroutine sessionRoutine;
        private Renderer driftIndicator;
        private Renderer orbitIndicator;
        private Renderer nebulaIndicator;

        public CabinMode CurrentMode => mode;

        private void Awake()
        {
            if (starWindow == null)
            {
                starWindow = FindAnyObjectByType<StarWindowSurface>();
            }

            if (starfieldWindow == null)
            {
                starfieldWindow = FindAnyObjectByType<StarfieldWindow>();
            }

            if (audioController == null)
            {
                audioController = FindAnyObjectByType<AmbientAudioController>();
            }

            // Console mode strips arrive with the Milestone 2 furniture pass;
            // these stay null-safe until then.
            driftIndicator = FindRenderer("Amber Mode Strip");
            orbitIndicator = FindRenderer("Teal Mode Strip");
            nebulaIndicator = FindRenderer("Blue Status Strip");
        }

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
            Debug.Log($"Starship Cabin ambience mode: {mode}");
        }

        public void CycleMode()
        {
            var modeCount = System.Enum.GetValues(typeof(CabinMode)).Length;
            var nextMode = (CabinMode)(((int)mode + 1) % modeCount);
            SetMode(nextMode);
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
            switch (nextMode)
            {
                case CabinMode.Drift:
                    if (starWindow != null)
                    {
                        starWindow.SetMotion(0.018f, 0.0f);
                        starWindow.SetNebula(0f);
                    }
                    starfieldWindow?.SetMotion(0.018f, 0.0f);
                    audioController?.SetMasterCalmVolume(1.0f);
                    break;
                case CabinMode.Orbit:
                    if (starWindow != null)
                    {
                        starWindow.SetMotion(0.095f, 0.020f);
                        starWindow.SetNebula(0f);
                    }
                    starfieldWindow?.SetMotion(0.095f, 0.020f);
                    audioController?.SetMasterCalmVolume(0.88f);
                    break;
                case CabinMode.Nebula:
                    if (starWindow != null)
                    {
                        starWindow.SetMotion(0.006f, 0.004f);
                        starWindow.SetNebula(1f);
                    }
                    starfieldWindow?.SetMotion(0.006f, 0.004f);
                    audioController?.SetMasterCalmVolume(0.74f);
                    break;
            }

            UpdateModeIndicators(nextMode);
        }

        private static Renderer FindRenderer(string objectName)
        {
            var found = GameObject.Find(objectName);
            return found == null ? null : found.GetComponent<Renderer>();
        }

        private static void SetIndicator(Renderer indicator, bool active)
        {
            if (indicator == null)
            {
                return;
            }

            var material = indicator.material;
            var baseColor = material.color;
            var intensity = active ? 2.8f : 0.35f;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", baseColor * intensity);
            indicator.transform.localScale = active
                ? new Vector3(indicator.transform.localScale.x, 0.075f, indicator.transform.localScale.z)
                : new Vector3(indicator.transform.localScale.x, 0.045f, indicator.transform.localScale.z);
        }

        private void UpdateModeIndicators(CabinMode activeMode)
        {
            SetIndicator(driftIndicator, activeMode == CabinMode.Drift);
            SetIndicator(orbitIndicator, activeMode == CabinMode.Orbit);
            SetIndicator(nebulaIndicator, activeMode == CabinMode.Nebula);
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
