using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Updates the restrained, world-space selector on the low table.</summary>
    public sealed class QuietWatchSelectorPanel : MonoBehaviour
    {
        [SerializeField] private VistaDirector director;
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh subtitle;
        [SerializeField] private TextMesh state;

        private bool previewNoticeWasActive;

        public void Configure(VistaDirector vistaDirector, TextMesh titleText, TextMesh subtitleText, TextMesh stateText)
        {
            if (director != null)
            {
                director.StateChanged -= Refresh;
            }
            director = vistaDirector;
            title = titleText;
            subtitle = subtitleText;
            state = stateText;
            if (isActiveAndEnabled && director != null)
            {
                director.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            // The preview acknowledgement expires without another state
            // change. Refresh only at that boundary to avoid per-frame text
            // allocations on the headset.
            var previewNoticeActive = director != null && director.IsPreviewNoticeActive;
            if (previewNoticeActive != previewNoticeWasActive)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var vista = director?.ActiveVista;
            if (title != null)
            {
                title.text = vista == null ? "THE QUIET WATCH" : vista.DisplayName;
            }
            if (subtitle != null)
            {
                subtitle.text = vista == null ? "SELECTING VIEW" : vista.Subtitle;
            }
            if (state != null && director != null)
            {
                previewNoticeWasActive = director.IsPreviewNoticeActive;
                var status = previewNoticeWasActive
                    ? "EVENT PREVIEW"
                    : $"{director.Life.ToString().ToUpperInvariant()} / {director.Motion.ToString().ToUpperInvariant()}";
                state.text = $"{status}\nA VISTA   TAP B LIFE\nHOLD B EVENT   STICK MOTION";
            }
        }
    }
}
