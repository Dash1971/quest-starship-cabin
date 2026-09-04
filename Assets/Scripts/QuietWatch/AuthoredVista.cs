using System;
using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    public enum AuthoredVistaKind
    {
        Harbour,
        BlueMorning,
        GreatWeather,
        LongFormation
    }

    /// <summary>
    /// Destination-specific exterior choreography. Quiet/Living controls how
    /// inhabited a vista feels; Still/Drift remains a comfort choice and never
    /// freezes objects whose physical situation requires them to move.
    /// </summary>
    public sealed class AuthoredVista : VistaEnvironment
    {
        [SerializeField] private AuthoredVistaKind kind;
        [SerializeField] private StarWindowSurface starWindow;
        [SerializeField] private Light exteriorFill;
        [SerializeField] private AmbientAudioController audioController;
        [SerializeField] private Transform slowTurn;
        [SerializeField] private Transform[] travellers;
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField, Min(15f)] private float graceNoteAtSeconds = 45f;

        private Vector3[] travellerOrigins = Array.Empty<Vector3>();
        private Quaternion[] travellerRotations = Array.Empty<Quaternion>();
        private Vector3[] travellerScales = Array.Empty<Vector3>();
        private HarbourTrafficRoute[] harbourRoutes = Array.Empty<HarbourTrafficRoute>();
        private ShipEnginePulse[] formationEngines = Array.Empty<ShipEnginePulse>();
        private Vector3 slowTurnOriginPosition;
        private Quaternion slowTurnOriginRotation;
        private Renderer heroRenderer;
        private MaterialPropertyBlock heroBlock;
        private LifeMode lifeMode;
        private MotionMode motionMode;
        private float enteredAt;
        private bool graceNotePlayed;
        private bool active;

        public float GraceNoteAtSeconds => graceNoteAtSeconds;
        public float GraceDurationSeconds => GraceDuration();

        public void Configure(
            string id,
            string title,
            string description,
            AuthoredVistaKind vistaKind,
            StarWindowSurface window,
            Light fill,
            AmbientAudioController audio,
            Transform rotatingElement,
            Transform[] movingElements,
            Color lightColor)
        {
            ConfigureIdentity(id, title, description);
            kind = vistaKind;
            starWindow = window;
            exteriorFill = fill;
            audioController = audio;
            slowTurn = rotatingElement;
            travellers = movingElements ?? Array.Empty<Transform>();
            fillColor = lightColor;
            graceNoteAtSeconds = GraceDelayFor(vistaKind);
            CacheOrigins();
        }

        private void Awake()
        {
            CacheOrigins();
        }

        private void CacheOrigins()
        {
            if (slowTurn != null)
            {
                slowTurnOriginPosition = slowTurn.localPosition;
                slowTurnOriginRotation = slowTurn.localRotation;
            }

            if (travellers == null)
            {
                travellers = Array.Empty<Transform>();
            }

            travellerOrigins = new Vector3[travellers.Length];
            travellerRotations = new Quaternion[travellers.Length];
            travellerScales = new Vector3[travellers.Length];
            harbourRoutes = new HarbourTrafficRoute[travellers.Length];
            formationEngines = new ShipEnginePulse[travellers.Length];
            for (var i = 0; i < travellers.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                travellerOrigins[i] = traveller.localPosition;
                travellerRotations[i] = traveller.localRotation;
                travellerScales[i] = traveller.localScale;
                harbourRoutes[i] = traveller.GetComponent<HarbourTrafficRoute>();
                formationEngines[i] = traveller.GetComponent<ShipEnginePulse>();
            }

            heroRenderer = slowTurn != null ? slowTurn.GetComponent<Renderer>() : null;
            heroBlock ??= new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            var elapsed = Time.unscaledTime - enteredAt;
            if (!graceNotePlayed && lifeMode == LifeMode.Living && elapsed >= graceNoteAtSeconds)
            {
                graceNotePlayed = true;
                audioController?.TriggerQuietWatchGrace(VistaId);
                Debug.Log($"Quiet Watch grace note started: {DisplayName}");
            }

            var grace = graceNotePlayed
                ? Smooth01((elapsed - graceNoteAtSeconds) / GraceDuration())
                : 0f;

            UpdateComposition(elapsed, grace);
        }

        /// <summary>
        /// Editor capture hook for deterministic inspection of Living motion
        /// and grace-note positions without waiting in real time.
        /// </summary>
        public void PreviewAt(float elapsed, LifeMode previewLifeMode, MotionMode previewMotionMode)
        {
            RestoreTransforms();
            lifeMode = previewLifeMode;
            motionMode = previewMotionMode;
            var grace = previewLifeMode == LifeMode.Living
                ? Smooth01((elapsed - graceNoteAtSeconds) / GraceDuration())
                : 0f;
            UpdateComposition(Mathf.Max(0f, elapsed), grace);
        }

        private void UpdateComposition(float elapsed, float grace)
        {
            switch (kind)
            {
                case AuthoredVistaKind.Harbour:
                    UpdateHarbourTraffic(elapsed, grace);
                    break;
                case AuthoredVistaKind.BlueMorning:
                    UpdateBlueMorning(elapsed, grace);
                    break;
                case AuthoredVistaKind.GreatWeather:
                    UpdateGreatWeather(elapsed, grace);
                    break;
                case AuthoredVistaKind.LongFormation:
                    UpdateFormation(elapsed, grace);
                    break;
            }

            UpdateCabinResponse(elapsed, grace);
        }

        public override void Enter(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            CacheOrigins();
            RestoreTransforms();
            active = true;
            enteredAt = Time.unscaledTime;
            graceNotePlayed = false;
            starWindow?.ResetVistaClock();
            ApplyComfort(nextLifeMode, nextMotionMode);

            if (exteriorFill != null)
            {
                exteriorFill.color = fillColor;
                exteriorFill.intensity = BaseFillIntensity();
            }
        }

        public override void ApplyComfort(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            lifeMode = nextLifeMode;
            motionMode = nextMotionMode;
            starWindow?.SetAuthoredVistaBackdrop(BackdropDensity());
            audioController?.SetQuietWatchProfile(VistaId, nextLifeMode == LifeMode.Living);

            if (kind == AuthoredVistaKind.Harbour)
            {
                // Quiet retains one remote lane. Living adds the upper transit
                // lane and the cutter that waits visibly at its berth until
                // the authored departure begins.
                for (var i = 0; i < travellerOrigins.Length; i++)
                {
                    var route = i < harbourRoutes.Length ? harbourRoutes[i] : null;
                    SetTravellerActive(i, route != null
                        && (nextLifeMode == LifeMode.Living || route.AvailableInQuiet));
                }
            }
        }

        public override void Exit()
        {
            active = false;
            RestoreTransforms();
            gameObject.SetActive(false);
        }

        private void UpdateHarbourTraffic(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var breathing = Mathf.Sin(elapsed * 0.015f) * 0.035f;
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.Euler(0f, 0f, breathing);
            }

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var route = i < harbourRoutes.Length ? harbourRoutes[i] : null;
                if (route == null)
                {
                    SetTravellerActive(i, false);
                    continue;
                }

                var living = lifeMode == LifeMode.Living;
                var shouldRun = living || route.AvailableInQuiet;
                SetTravellerActive(i, shouldRun);
                if (!shouldRun || travellers[i] == null)
                {
                    continue;
                }

                // The grace-route cutter remains physically docked at point 0
                // until its one departure. Other lanes loop only while both
                // endpoints are outside the useful window area.
                var phase = route.IsGraceRoute ? grace : route.PhaseAt(elapsed, living);
                route.Evaluate(phase, out var position, out var tangent, out var curvature);
                travellers[i].localPosition = position;

                // Blender vessels point down local -Z. Align that nose with the
                // spline tangent and bank into curvature; no sideways sliding.
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    tangent.Normalize();
                    var bank = Mathf.Clamp(curvature * route.BankDegrees, -route.BankDegrees, route.BankDegrees);
                    travellers[i].localRotation = Quaternion.AngleAxis(bank, tangent)
                        * Quaternion.LookRotation(-tangent, Vector3.up);
                }
            }
        }

        private void UpdateFormation(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var living = lifeMode == LifeMode.Living;
                // The whole formation is underway in both modes. A broad,
                // bounded flight curve gives an immediately readable change
                // against the window while preserving a restful composition.
                var activity = living ? 1.0f : 0.72f;
                var cruise = new Vector3(
                    Mathf.Sin(elapsed * 0.012f) * 2.2f * activity,
                    Mathf.Sin(elapsed * 0.008f + 0.5f) * 0.62f * activity,
                    Mathf.Sin(elapsed * 0.012f) * 3.8f * activity);
                var courseYaw = Mathf.Cos(elapsed * 0.012f) * 1.35f * activity;
                var turn = Quaternion.Euler(
                    -2.0f * grace,
                    courseYaw - 10.0f * grace,
                    3.4f * Mathf.Sin(grace * Mathf.PI));
                slowTurn.localPosition = slowTurnOriginPosition + cruise;
                slowTurn.localRotation = slowTurnOriginRotation * turn;
            }

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                traveller.gameObject.SetActive(true);
                var living = lifeMode == LifeMode.Living;
                var phase = elapsed * (0.082f + i * 0.011f) + i * 2.1f;
                var correctionScale = living ? 1.0f : 0.58f;
                var correction = new Vector3(
                    Mathf.Sin(phase) * (0.16f + i * 0.035f),
                    Mathf.Sin(phase * 0.71f) * (0.10f + i * 0.018f),
                    Mathf.Cos(phase * 0.53f) * (0.12f + i * 0.025f)) * correctionScale;
                traveller.localPosition = travellerOrigins[i] + correction;
                traveller.localRotation = travellerRotations[i]
                    * Quaternion.Euler(
                        Mathf.Sin(phase * 0.71f) * 0.38f * correctionScale,
                        Mathf.Sin(phase * 0.6f) * 0.72f * correctionScale,
                        Mathf.Sin(phase) * 1.10f * correctionScale);
                if (i < formationEngines.Length && formationEngines[i] != null)
                {
                    formationEngines[i].SetActivity(living ? 1.0f : 0.68f);
                }
            }
        }

        private void UpdateGreatWeather(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var degrees = elapsed * (motionMode == MotionMode.Drift ? 0.035f : 0.010f);
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.AngleAxis(degrees, Vector3.up);
            }

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                var distanceScale = i == 0 ? 1f : 0.45f;
                var emergence = new Vector3(12f, 4.2f, -2f) * grace * distanceScale;
                traveller.localPosition = travellerOrigins[i] + emergence;
            }
            SetHeroFloat("_WeatherPulse", grace);
        }

        private void UpdateBlueMorning(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var degrees = motionMode == MotionMode.Drift ? elapsed * 0.028f : 0f;
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.AngleAxis(degrees, Vector3.up);
            }
            SetHeroFloat("_DawnProgress", grace);
        }

        private void RestoreTransforms()
        {
            if (slowTurn != null)
            {
                slowTurn.localPosition = slowTurnOriginPosition;
                slowTurn.localRotation = slowTurnOriginRotation;
            }

            SetHeroFloat("_DawnProgress", 0f);
            SetHeroFloat("_WeatherPulse", 0f);

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                traveller.localPosition = travellerOrigins[i];
                traveller.localRotation = travellerRotations[i];
                traveller.localScale = travellerScales[i];
                traveller.gameObject.SetActive(true);
            }
        }

        private void SetTravellerActive(int index, bool value)
        {
            if (index >= 0 && index < travellers.Length && travellers[index] != null
                && travellers[index].gameObject.activeSelf != value)
            {
                travellers[index].gameObject.SetActive(value);
            }
        }

        private void UpdateCabinResponse(float elapsed, float grace)
        {
            if (exteriorFill == null)
            {
                return;
            }

            var color = fillColor;
            var intensity = BaseFillIntensity();
            switch (kind)
            {
                case AuthoredVistaKind.Harbour:
                    var harbourPulse = lifeMode == LifeMode.Living
                        ? 0.018f * (0.5f + 0.5f * Mathf.Sin(elapsed * 0.19f))
                        : 0f;
                    color = Color.Lerp(fillColor, new Color(0.46f, 0.78f, 1.0f), 0.24f + grace * 0.16f);
                    intensity += harbourPulse + grace * 0.055f;
                    break;
                case AuthoredVistaKind.BlueMorning:
                    color = Color.Lerp(fillColor, new Color(1.0f, 0.72f, 0.48f), grace);
                    intensity += grace * 0.34f;
                    break;
                case AuthoredVistaKind.GreatWeather:
                    var stormBreath = 0.018f * (0.5f + 0.5f * Mathf.Sin(elapsed * 0.055f));
                    color = Color.Lerp(fillColor, new Color(1.0f, 0.60f, 0.34f), grace * 0.42f);
                    intensity += stormBreath + grace * 0.12f;
                    break;
                case AuthoredVistaKind.LongFormation:
                    intensity += grace * 0.045f;
                    break;
            }

            exteriorFill.color = color;
            exteriorFill.intensity = intensity;
        }

        private float BaseFillIntensity()
        {
            switch (kind)
            {
                case AuthoredVistaKind.BlueMorning: return 0.64f;
                case AuthoredVistaKind.GreatWeather: return 0.44f;
                case AuthoredVistaKind.LongFormation: return 0.38f;
                default: return 0.42f;
            }
        }

        private float BackdropDensity()
        {
            switch (kind)
            {
                case AuthoredVistaKind.BlueMorning: return 0.24f;
                case AuthoredVistaKind.GreatWeather: return 0.38f;
                case AuthoredVistaKind.LongFormation: return 0.50f;
                default: return 0.52f;
            }
        }

        private float GraceDuration()
        {
            switch (kind)
            {
                case AuthoredVistaKind.Harbour: return 72f;
                case AuthoredVistaKind.BlueMorning: return 110f;
                case AuthoredVistaKind.GreatWeather: return 96f;
                case AuthoredVistaKind.LongFormation: return 84f;
                default: return 90f;
            }
        }

        private static float GraceDelayFor(AuthoredVistaKind vistaKind)
        {
            switch (vistaKind)
            {
                case AuthoredVistaKind.Harbour: return 720f;
                case AuthoredVistaKind.BlueMorning: return 840f;
                case AuthoredVistaKind.GreatWeather: return 900f;
                case AuthoredVistaKind.LongFormation: return 780f;
                default: return 780f;
            }
        }

        private void SetHeroFloat(string propertyName, float value)
        {
            if (heroRenderer == null)
            {
                return;
            }
            heroRenderer.GetPropertyBlock(heroBlock);
            heroBlock.SetFloat(propertyName, value);
            heroRenderer.SetPropertyBlock(heroBlock);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }

}
