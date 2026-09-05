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
        [SerializeField, Min(15f)] private float graceNoteAtSeconds = 780f;

        private LifeMode lifeMode;
        private VistaTimeline timeline;
        private bool paused;
        private bool focused = true;
        private bool active;

        public void Configure(StarWindowSurface window, Light fill, AmbientAudioController audio)
        {
            starWindow = window;
            exteriorFill = fill;
            audioController = audio;
        }

        private void Update()
        {
            if (!active || paused || !focused) return;
            if (timeline.Advance(Mathf.Min(Time.unscaledDeltaTime, 0.1f)))
            {
                starWindow?.TriggerFirstQuestionComet();
                audioController?.TriggerQuietWatchGrace(VistaId);
            }
            starWindow?.SetGraceAge((float)timeline.EventAge);
        }

        private void OnApplicationPause(bool value) => paused = value;
        private void OnApplicationFocus(bool value) => focused = value;

        public void PreviewAt(float elapsed, LifeMode life, MotionMode motion)
        {
            starWindow?.PreviewAt(elapsed, motion == MotionMode.Drift,
                life == LifeMode.Living && elapsed >= graceNoteAtSeconds ? elapsed - graceNoteAtSeconds : -1f);
        }

        public override void Enter(LifeMode nextLifeMode, MotionMode motionMode)
        {
            active = true;
            timeline = new VistaTimeline(graceNoteAtSeconds, 8);
            timeline.Reset(nextLifeMode == LifeMode.Living, motionMode == MotionMode.Drift);
            lifeMode = nextLifeMode;
            starWindow?.ResetVistaClock();
            ApplyComfort(nextLifeMode, motionMode);

            if (exteriorFill != null)
            {
                exteriorFill.color = new Color(0.38f, 0.52f, 0.78f);
                exteriorFill.intensity = 0.38f;
            }

            audioController?.SetQuietWatchProfile("first-question", nextLifeMode == LifeMode.Living);
        }

        public override void ApplyComfort(LifeMode nextLifeMode, MotionMode motionMode)
        {
            if (lifeMode != nextLifeMode)
            {
                if (nextLifeMode == LifeMode.Quiet) audioController?.CancelQuietWatchGrace();
            }
            lifeMode = nextLifeMode;
            timeline?.SetModes(nextLifeMode == LifeMode.Living, motionMode == MotionMode.Drift);
            starWindow?.SetQuietWatchComfort(
                living: nextLifeMode == LifeMode.Living,
                drifting: motionMode == MotionMode.Drift);
            audioController?.SetQuietWatchProfile("first-question", nextLifeMode == LifeMode.Living);
        }

        public override void Exit()
        {
            active = false;
            audioController?.CancelQuietWatchGrace();
            starWindow?.ClearGraceNote();
            gameObject.SetActive(false);
        }

        public override bool PreviewGraceNote()
        {
            if (!active || timeline == null || timeline.EventAge >= 0 || !timeline.Preview(false)) return false;
            starWindow?.TriggerFirstQuestionComet();
            audioController?.TriggerQuietWatchGrace(VistaId);
            return true;
        }
    }
}
