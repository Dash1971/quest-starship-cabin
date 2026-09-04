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
        private Vector3 slowTurnOriginPosition;
        private Quaternion slowTurnOriginRotation;
        private LifeMode lifeMode;
        private MotionMode motionMode;
        private float enteredAt;
        private bool graceNotePlayed;
        private bool active;

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
            }
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
            starWindow?.SetAuthoredVistaBackdrop(kind == AuthoredVistaKind.BlueMorning ? 0.34f : 0.58f);
            audioController?.SetQuietWatchProfile(nextLifeMode == LifeMode.Living);

            if (kind == AuthoredVistaKind.Harbour)
            {
                // Quiet retains only a single distant shipping lane. Living
                // activates all three causal arrival/departure routes.
                for (var i = 0; i < travellerOrigins.Length; i++)
                {
                    SetTravellerActive(i, nextLifeMode == LifeMode.Living || i == travellerOrigins.Length - 1);
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
                var living = lifeMode == LifeMode.Living;
                var shouldRun = living || i == travellerOrigins.Length - 1;
                SetTravellerActive(i, shouldRun);
                if (!shouldRun || travellers[i] == null)
                {
                    continue;
                }

                // The routes begin and end beyond the useful window area, so
                // wrapping reads as separate traffic rather than teleporting.
                var duration = living ? 34f + i * 11f : 92f;
                var offset = living ? i * 0.31f : 0.19f;
                var phase = Mathf.Repeat(elapsed / duration + offset, 1f);
                var travel = Smooth01(phase);
                var origin = travellerOrigins[i];
                var start = HarbourOffset(i, true);
                var end = HarbourOffset(i, false);
                var arch = new Vector3(0f, Mathf.Sin(phase * Mathf.PI) * (1.8f + i), 0f);
                travellers[i].localPosition = origin + Vector3.Lerp(start, end, travel) + arch;

                var bank = Mathf.Sin(phase * Mathf.PI) * (i == 0 ? -7f : 4f);
                var yaw = (i == 0 ? -12f : i == 1 ? 8f : -5f) * Mathf.Sin(phase * Mathf.PI);
                travellers[i].localRotation = travellerRotations[i] * Quaternion.Euler(0f, yaw, bank);
            }

            // The review-timed grace note is a close customs cutter departing
            // the harbour mouth with its tender pacing it.
            if (grace > 0f && travellerOrigins.Length > 0 && travellers[0] != null)
            {
                SetTravellerActive(0, true);
                var eased = Smooth01(grace);
                travellers[0].localPosition = travellerOrigins[0]
                    + Vector3.Lerp(Vector3.zero, new Vector3(-10f, 6f, -5f), eased);
                travellers[0].localRotation = travellerRotations[0]
                    * Quaternion.Euler(-3f * eased, 13f * eased, 8f * Mathf.Sin(eased * Mathf.PI));
            }
        }

        private void UpdateFormation(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var living = lifeMode == LifeMode.Living;
                var cruise = living
                    ? new Vector3(Mathf.Sin(elapsed * 0.035f) * 0.22f, Mathf.Sin(elapsed * 0.021f) * 0.10f, -elapsed * 0.006f)
                    : Vector3.zero;
                var turn = Quaternion.Euler(-1.2f * grace, 8.5f * grace, -2.8f * Mathf.Sin(grace * Mathf.PI));
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
                if (lifeMode == LifeMode.Quiet)
                {
                    traveller.localPosition = travellerOrigins[i];
                    traveller.localRotation = travellerRotations[i];
                    continue;
                }

                var phase = elapsed * (0.045f + i * 0.008f) + i * 2.1f;
                var correction = new Vector3(
                    Mathf.Sin(phase) * (0.035f + i * 0.008f),
                    Mathf.Sin(phase * 0.71f) * 0.025f,
                    Mathf.Cos(phase * 0.53f) * 0.018f);
                traveller.localPosition = travellerOrigins[i] + correction;
                traveller.localRotation = travellerRotations[i]
                    * Quaternion.Euler(0f, Mathf.Sin(phase * 0.6f) * 0.28f, Mathf.Sin(phase) * 0.42f);
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

            if (exteriorFill != null)
            {
                exteriorFill.intensity = BaseFillIntensity() + grace * 0.12f;
            }
        }

        private void UpdateBlueMorning(float elapsed, float grace)
        {
            if (slowTurn != null)
            {
                var degrees = motionMode == MotionMode.Drift ? elapsed * 0.028f : 0f;
                slowTurn.localRotation = slowTurnOriginRotation * Quaternion.AngleAxis(degrees, Vector3.up);
            }

            if (exteriorFill != null)
            {
                exteriorFill.color = Color.Lerp(fillColor, new Color(1.0f, 0.72f, 0.48f), grace);
                exteriorFill.intensity = BaseFillIntensity() + grace * 0.34f;
            }
        }

        private void RestoreTransforms()
        {
            if (slowTurn != null)
            {
                slowTurn.localPosition = slowTurnOriginPosition;
                slowTurn.localRotation = slowTurnOriginRotation;
            }

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

        private static Vector3 HarbourOffset(int index, bool start)
        {
            switch (index)
            {
                case 0:
                    return start ? new Vector3(18f, 5f, -24f) : new Vector3(-15f, -2f, 12f);
                case 1:
                    return start ? new Vector3(-26f, -5f, -16f) : new Vector3(27f, 1f, 8f);
                default:
                    return start ? new Vector3(24f, 4f, -12f) : new Vector3(-35f, -2f, 10f);
            }
        }

        private float BaseFillIntensity()
        {
            return kind == AuthoredVistaKind.BlueMorning ? 0.72f : 0.48f;
        }

        private float GraceDuration()
        {
            return kind == AuthoredVistaKind.Harbour ? 24f : 32f;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
