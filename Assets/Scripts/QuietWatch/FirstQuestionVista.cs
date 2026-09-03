using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// M1 benchmark vista: true-black star space, long stillness, and one
    /// authored comet only when Living mode is selected.
    /// </summary>
    public sealed class FirstQuestionVista : VistaEnvironment
    {
        [SerializeField] private StarWindowSurface starWindow;
        [SerializeField] private Light exteriorFill;
        [SerializeField] private AmbientAudioController audioController;
        [SerializeField, Min(60f)] private float graceNoteAtSeconds = 12f * 60f;

        private LifeMode lifeMode;
        private float enteredAt;
        private bool graceNotePlayed;
        private bool active;

        public void Configure(StarWindowSurface window, Light fill, AmbientAudioController audio)
        {
            starWindow = window;
            exteriorFill = fill;
            audioController = audio;
        }

        private void Update()
        {
            if (!active || graceNotePlayed || lifeMode != LifeMode.Living)
            {
                return;
            }

            if (Time.unscaledTime - enteredAt >= graceNoteAtSeconds)
            {
                graceNotePlayed = true;
                starWindow?.TriggerFirstQuestionComet();
                Debug.Log("Quiet Watch grace note: First Question comet");
            }
        }

        public override void Enter(LifeMode nextLifeMode, MotionMode motionMode)
        {
            active = true;
            enteredAt = Time.unscaledTime;
            graceNotePlayed = false;
            starWindow?.ResetVistaClock();
            ApplyComfort(nextLifeMode, motionMode);

            if (exteriorFill != null)
            {
                exteriorFill.color = new Color(0.38f, 0.52f, 0.78f);
                exteriorFill.intensity = 0.38f;
            }

            audioController?.SetQuietWatchProfile(nextLifeMode == LifeMode.Living);
        }

        public override void ApplyComfort(LifeMode nextLifeMode, MotionMode motionMode)
        {
            lifeMode = nextLifeMode;
            starWindow?.SetQuietWatchComfort(
                living: nextLifeMode == LifeMode.Living,
                drifting: motionMode == MotionMode.Drift);
            audioController?.SetQuietWatchProfile(nextLifeMode == LifeMode.Living);
        }

        public override void Exit()
        {
            active = false;
            starWindow?.ClearGraceNote();
            gameObject.SetActive(false);
        }
    }
}
