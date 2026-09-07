using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>One quiet stellar field, coherent deskward travel and a distant two-minute comet.</summary>
    public sealed class FirstQuestionVista : VistaEnvironment
    {
        [SerializeField] private StarWindowSurface starWindow;
        [SerializeField] private Light exteriorFill;
        [SerializeField] private AmbientAudioController audioController;
        [SerializeField] private Renderer cruiseStars;
        [SerializeField] private Renderer distantComet;
        private MaterialPropertyBlock cruiseBlock, cometBlock;
        private static readonly int TravelId=Shader.PropertyToID("_Travel"), SectorId=Shader.PropertyToID("_Sector"),
            CometId=Shader.PropertyToID("_CometPosition");
        private LifeMode lifeMode;
        private VistaTimeline timeline;
        private bool paused, active;
        private bool focused=true;

        public void Configure(StarWindowSurface window, Light fill, AmbientAudioController audio, Renderer stars, Renderer comet)
        {
            cruiseStars=stars; distantComet=comet; starWindow=window; exteriorFill=fill; audioController=audio;
        }

        private void Update()
        {
            if(!active || paused || !focused) return;
            timeline.Advance(Mathf.Min(Time.unscaledDeltaTime,.1f));
            WriteField();
        }

        private void WriteField()
        {
            if(cruiseStars==null || timeline==null) return;
            cruiseBlock ??= new MaterialPropertyBlock();
            cruiseStars.GetPropertyBlock(cruiseBlock);
            var distance=timeline.DriftTravel*FirstQuestionField.Speed;
            cruiseBlock.SetFloat(TravelId,(float)(distance%FirstQuestionField.Period));
            cruiseBlock.SetFloat(SectorId,(float)System.Math.Floor(distance/FirstQuestionField.Period));
            cruiseStars.SetPropertyBlock(cruiseBlock);
            if(distantComet==null) return;
            var p=FirstQuestionField.CometAt(timeline.EventAge,timeline.DriftTravel-timeline.DriftAtEventStart);
            cometBlock ??= new MaterialPropertyBlock();
            distantComet.GetPropertyBlock(cometBlock);
            cometBlock.SetVector(CometId,new Vector4((float)p.X,(float)p.Y,(float)p.Z,(float)p.Visibility));
            distantComet.SetPropertyBlock(cometBlock);
        }

        private void OnApplicationPause(bool value)=>paused=value;
        private void OnApplicationFocus(bool value)=>focused=value;

        public void PreviewAt(float elapsed,LifeMode life,MotionMode motion)
        {
            timeline.Reset(life==LifeMode.Living,false);
            timeline.SetModes(life==LifeMode.Living,motion==MotionMode.Drift);
            timeline.Advance(Mathf.Max(0,elapsed));
            starWindow?.PreviewAt(elapsed,false,-1);
            WriteField();
        }

        public void PreviewCometAt(float elapsed,MotionMode motion)
        {
            PreviewAt(0,LifeMode.Living,motion);
            timeline.Preview(.30,false);
            timeline.Advance(Mathf.Max(0,elapsed));
            WriteField();
        }

        public override void Enter(LifeMode nextLifeMode,MotionMode motionMode)
        {
            active=true;
            timeline=new VistaTimeline(FirstQuestionField.CometDelay,FirstQuestionField.CometDuration);
            timeline.Reset(nextLifeMode==LifeMode.Living,false);
            lifeMode=nextLifeMode;
            starWindow?.ResetVistaClock();
            ApplyComfort(nextLifeMode,motionMode);
            if(exteriorFill!=null)
            {
                exteriorFill.color=new Color(.38f,.52f,.78f); exteriorFill.intensity=.38f;
            }
        }

        public override void ApplyComfort(LifeMode nextLifeMode,MotionMode motionMode)
        {
            if(lifeMode==LifeMode.Living && nextLifeMode==LifeMode.Quiet) audioController?.CancelQuietWatchGrace();
            lifeMode=nextLifeMode;
            timeline?.SetModes(nextLifeMode==LifeMode.Living,motionMode==MotionMode.Drift);
            starWindow?.SetQuietWatchComfort(nextLifeMode==LifeMode.Living,motionMode==MotionMode.Drift);
            audioController?.SetQuietWatchProfile("first-question",nextLifeMode==LifeMode.Living);
            WriteField();
        }

        public override void Exit()
        {
            active=false; audioController?.CancelQuietWatchGrace(); starWindow?.ClearGraceNote(); gameObject.SetActive(false);
        }

        public override bool PreviewGraceNote()
        {
            if(!active || timeline==null || !timeline.Preview(.30,false)) return false;
            // A distant comet has no arrival sound or alert; it belongs to the silence outside.
            WriteField(); return true;
        }
    }
}
