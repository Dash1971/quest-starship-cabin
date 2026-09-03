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
    /// Runtime behaviour shared by the four authored exterior compositions.
    /// Geometry and materials are built into the scene by the editor tool;
    /// this component only owns lifecycle, comfort motion, light response,
    /// and the single late-session grace note.
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
        [SerializeField, Min(60f)] private float graceNoteAtSeconds = 12f * 60f;

        private Vector3[] travellerOrigins;
        private Quaternion slowTurnOrigin;
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
            travellers = movingElements;
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
                slowTurnOrigin = slowTurn.localRotation;
            }

            if (travellers == null)
            {
                travellerOrigins = System.Array.Empty<Vector3>();
                return;
            }

            travellerOrigins = new Vector3[travellers.Length];
            for (var i = 0; i < travellers.Length; i++)
            {
                travellerOrigins[i] = travellers[i] == null ? Vector3.zero : travellers[i].localPosition;
            }
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            var elapsed = Time.unscaledTime - enteredAt;
            var motionScale = motionMode == MotionMode.Drift ? 1f : 0.22f;

            if (slowTurn != null)
            {
                var degrees = elapsed * RotationRate() * motionScale;
                slowTurn.localRotation = slowTurnOrigin * Quaternion.AngleAxis(degrees, RotationAxis());
            }

            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                var traveller = travellers[i];
                if (traveller == null)
                {
                    continue;
                }

                var phase = elapsed * (0.035f + i * 0.006f) + i * 1.7f;
                var amplitude = motionMode == MotionMode.Drift ? 0.42f : 0.12f;
                traveller.localPosition = travellerOrigins[i]
                    + new Vector3(Mathf.Sin(phase) * amplitude, Mathf.Cos(phase * 0.73f) * amplitude * 0.35f, 0f);
            }

            if (!graceNotePlayed && lifeMode == LifeMode.Living && elapsed >= graceNoteAtSeconds)
            {
                graceNotePlayed = true;
                Debug.Log($"Quiet Watch grace note: {DisplayName}");
            }
        }

        public override void Enter(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            CacheOrigins();
            active = true;
            enteredAt = Time.unscaledTime;
            graceNotePlayed = false;
            starWindow?.ResetVistaClock();
            starWindow?.SetAuthoredVistaBackdrop(kind == AuthoredVistaKind.BlueMorning ? 0.42f : 0.72f);
            ApplyComfort(nextLifeMode, nextMotionMode);

            if (exteriorFill != null)
            {
                exteriorFill.color = fillColor;
                exteriorFill.intensity = kind == AuthoredVistaKind.BlueMorning ? 0.72f : 0.48f;
            }
        }

        public override void ApplyComfort(LifeMode nextLifeMode, MotionMode nextMotionMode)
        {
            lifeMode = nextLifeMode;
            motionMode = nextMotionMode;
            starWindow?.SetAuthoredVistaBackdrop(kind == AuthoredVistaKind.BlueMorning ? 0.42f : 0.72f);
            audioController?.SetQuietWatchProfile(nextLifeMode == LifeMode.Living);
        }

        public override void Exit()
        {
            active = false;
            if (slowTurn != null)
            {
                slowTurn.localRotation = slowTurnOrigin;
            }
            if (travellerOrigins == null || travellers == null)
            {
                gameObject.SetActive(false);
                return;
            }
            for (var i = 0; i < travellerOrigins.Length; i++)
            {
                if (travellers[i] != null)
                {
                    travellers[i].localPosition = travellerOrigins[i];
                }
            }
            gameObject.SetActive(false);
        }

        private float RotationRate()
        {
            return kind switch
            {
                AuthoredVistaKind.Harbour => 0.22f,
                AuthoredVistaKind.BlueMorning => 0.035f,
                AuthoredVistaKind.GreatWeather => 0.11f,
                AuthoredVistaKind.LongFormation => 0.045f,
                _ => 0f
            };
        }

        private Vector3 RotationAxis()
        {
            return kind == AuthoredVistaKind.BlueMorning ? Vector3.up : Vector3.forward;
        }
    }
}
