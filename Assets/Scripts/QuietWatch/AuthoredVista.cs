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

        [SerializeField] private Vector3 moonEmergence;
        public void ConfigureMoonEmergence(Vector3 displacement) => moonEmergence = displacement;

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
        private VistaTimeline timeline;
        private VistaBackdropLayers backdropLayers;
        private GreatWeatherEclipse eclipse;
        private bool originsCached;
        private bool paused;
        private bool focused = true;
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
            originsCached = false;
            CacheOrigins();
        }

        private void Awake()
        {
            CacheOrigins();
        }

        private void CacheOrigins()
        {
            if (originsCached) return;
            originsCached = true;
            backdropLayers = GetComponent<VistaBackdropLayers>();
            eclipse = GetComponent<GreatWeatherEclipse>();
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

            if (paused || !focused) return;
            // Do not catch up a suspended app or a large stall in one visible frame.
            if (timeline.Advance(Mathf.Min(Time.unscaledDeltaTime, 0.1f), AllowEventMotion))
                audioController?.TriggerQuietWatchGrace(VistaId);
            UpdateComposition((float)timeline.Elapsed, (float)timeline.Progress);
        }

        // Still/Drift controls optional comfort motion. It must not freeze
        // physically underway traffic, ships, or their authored grace notes.
        private const bool AllowEventMotion = true;

        private void OnApplicationPause(bool value) => paused = value;
        private void OnApplicationFocus(bool value) => focused = value;

        /// <summary>
        /// Editor capture hook for deterministic inspection of Living motion
        /// and grace-note positions without waiting in real time.
        /// </summary>
        public void PreviewAt(float elapsed, LifeMode previewLifeMode, MotionMode previewMotionMode)
        {
            CacheOrigins();
            RestoreTransforms();
            lifeMode = previewLifeMode;
            motionMode = previewMotionMode;
            timeline ??= new VistaTimeline(graceNoteAtSeconds, GraceDuration());
            timeline.Seek(elapsed, lifeMode == LifeMode.Living, motionMode == MotionMode.Drift, AllowEventMotion);
            ApplyComfort(previewLifeMode, previewMotionMode);
            starWindow?.PreviewAt(elapsed, false, -1f);
            UpdateComposition((float)timeline.Elapsed, (float)timeline.Progress);
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

            backdropLayers?.EvaluateAt(elapsed, kind == AuthoredVistaKind.BlueMorning ? grace : 0f);
            SetHeroFloat("_ObservationTime", elapsed);
            UpdateCabinResponse(elapsed, grace);
        }

        public override void Enter(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            CacheOrigins();
            RestoreTransforms();
            active = true;
            timeline = new VistaTimeline(graceNoteAtSeconds, GraceDuration());
            timeline.Reset(nextLifeMode == LifeMode.Living, nextMotionMode == MotionMode.Drift);
            lifeMode = nextLifeMode;
            motionMode = nextMotionMode;
            starWindow?.ResetVistaClock();
            ApplyComfort(nextLifeMode, nextMotionMode);

            if (exteriorFill != null)
            {
                exteriorFill.color = fillColor;
                exteriorFill.intensity = BaseFillIntensity();
            }
            UpdateComposition(0f, 0f);
        }

        public override void ApplyComfort(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            var cancelEvent = lifeMode == LifeMode.Living && nextLifeMode == LifeMode.Quiet;
            lifeMode = nextLifeMode;
            motionMode = nextMotionMode;
            timeline?.SetModes(nextLifeMode == LifeMode.Living, nextMotionMode == MotionMode.Drift);
            if (cancelEvent)
            {
                audioController?.CancelQuietWatchGrace();
                if (active && timeline != null)
                    UpdateComposition((float)timeline.Elapsed, 0f);
            }

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
            audioController?.CancelQuietWatchGrace();
            RestoreTransforms();
            gameObject.SetActive(false);
        }

        public override bool PreviewGraceNote()
        {
            if (!active || timeline == null || !timeline.Preview()) return false;
            audioController?.TriggerQuietWatchGrace(VistaId);
            UpdateComposition((float)timeline.Elapsed, (float)timeline.Progress);
            Debug.Log($"Quiet Watch event preview started: {DisplayName}");
            return true;
        }

        private void UpdateHarbourTraffic(float elapsed, float grace)
        {
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
                // until its one departure. Service traffic eases into its berth
                // and backs out after a dwell; integrated clocks preserve pose
                // when switching Quiet/Living.
                var phase = route.IsGraceRoute ? grace : route.PhaseFromTravel(
                    timeline.LivingTravel / route.LivingDuration + timeline.QuietTravel / route.QuietDuration);
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
                // The formation remains visibly underway in both Quiet and
                // Living, including default Still. Integrated clocks preserve
                // its pose when Life mode changes instead of rebasing motion.
                var flightPhase = (float)(timeline.LivingTravel * 0.064
                    + timeline.QuietTravel * 0.052);
                var cruise = new Vector3(
                    Mathf.Sin(flightPhase) * 3.8f,
                    Mathf.Sin(flightPhase * 0.63f + 0.5f) * 1.05f,
                    Mathf.Sin(flightPhase * 0.83f - 0.35f) * 5.8f);
                var courseYaw = Mathf.Cos(flightPhase) * 2.8f;
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
                var stationTime = (float)(timeline.LivingTravel + timeline.QuietTravel * 0.80);
                var phase = stationTime * (0.118f + i * 0.015f) + i * 2.1f;
                var correctionScale = Mathf.Lerp(0.72f, 1f, (float)timeline.Activity);
                var correction = new Vector3(
                    Mathf.Sin(phase) * (0.38f + i * 0.075f),
                    Mathf.Sin(phase * 0.71f) * (0.22f + i * 0.040f),
                    Mathf.Cos(phase * 0.53f) * (0.28f + i * 0.055f)) * correctionScale;
                traveller.localPosition = travellerOrigins[i] + correction;
                traveller.localRotation = travellerRotations[i]
                    * Quaternion.Euler(
                        Mathf.Sin(phase * 0.71f) * 0.85f * correctionScale,
                        Mathf.Sin(phase * 0.6f) * 1.45f * correctionScale,
                        Mathf.Sin(phase) * 2.10f * correctionScale);
                if (i < formationEngines.Length && formationEngines[i] != null)
                {
                    formationEngines[i].SetActivity(Mathf.Lerp(0.68f, 1f, (float)timeline.Activity));
                    formationEngines[i].EvaluateAt(elapsed);
                }
            }
        }

        private void UpdateGreatWeather(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                // The weather keeps its geological motion in Still; Drift adds
                // speed without rebasing the already observed pose.
                var stillTravel = timeline.Elapsed - timeline.DriftTravel;
                var degrees = (float)(stillTravel * 0.010 + timeline.DriftTravel * 0.035);
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.AngleAxis(degrees, Vector3.up);
            }

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                var emergence = i == 0 ? moonEmergence * grace : Vector3.zero;
                traveller.localPosition = travellerOrigins[i] + emergence;
            }
            eclipse?.EvaluateAt(grace);
            SetHeroFloat("_WeatherPulse", 0f);
        }

        private void UpdateBlueMorning(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var degrees = (float)timeline.DriftTravel * 0.028f;
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.AngleAxis(degrees, Vector3.up);
            }
            SetHeroFloat("_DawnProgress", grace);
        }

        private void RestoreTransforms()
        {
            backdropLayers?.EvaluateAt(0f, 0f);
            eclipse?.EvaluateAt(0f);
            if (slowTurn != null)
            {
                slowTurn.localPosition = slowTurnOriginPosition;
                slowTurn.localRotation = slowTurnOriginRotation;
            }

            SetHeroFloat("_ObservationTime", 0f);
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
                    // A working port has steady practical lighting. Traffic and
                    // the departure event must not pulse or recolour the cabin.
                    color = fillColor;
                    break;
                case AuthoredVistaKind.BlueMorning:
                    color = Color.Lerp(fillColor, new Color(1.0f, 0.72f, 0.48f), grace);
                    intensity += grace * 0.34f;
                    break;
                case AuthoredVistaKind.GreatWeather:
                    // A distant moon emerging from shadow cannot relight an
                    // entire cabin. Keep the giant's reflected fill stable.
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
                case AuthoredVistaKind.GreatWeather: return 360f;
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
