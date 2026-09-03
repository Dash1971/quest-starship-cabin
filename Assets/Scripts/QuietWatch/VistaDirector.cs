using System;
using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Owns deterministic vista entry/exit and the three player decisions:
    /// vista, Quiet/Living, and Still/Drift.
    /// </summary>
    public sealed class VistaDirector : MonoBehaviour
    {
        [SerializeField] private VistaEnvironment[] vistas;
        [SerializeField] private ScreenFader screenFader;

        private int activeIndex;
        private VistaEnvironment activeVista;

        public event Action StateChanged;

        public LifeMode Life { get; private set; }
        public MotionMode Motion { get; private set; }
        public VistaEnvironment ActiveVista => activeVista;
        public int VistaCount => vistas?.Length ?? 0;

        public void Configure(VistaEnvironment[] availableVistas, ScreenFader fader)
        {
            vistas = availableVistas;
            screenFader = fader;
        }

        private void Start()
        {
            if (vistas == null || vistas.Length == 0)
            {
                Debug.LogError("Quiet Watch has no registered vistas.");
                return;
            }

            Life = QuietWatchSettings.LoadLife();
            Motion = QuietWatchSettings.LoadMotion();
            var savedId = QuietWatchSettings.LoadVista(vistas[0].VistaId);
            activeIndex = FindVista(savedId);

            for (var i = 0; i < vistas.Length; i++)
            {
                if (vistas[i] != null)
                {
                    vistas[i].gameObject.SetActive(i == activeIndex);
                }
            }

            activeVista = vistas[activeIndex];
            activeVista.Enter(Life, Motion);
            SaveAndNotify();
        }

        public void SelectNextVista()
        {
            if (vistas == null || vistas.Length < 2)
            {
                StateChanged?.Invoke();
                return;
            }

            SelectVista((activeIndex + 1) % vistas.Length);
        }

        public void ToggleLifeMode()
        {
            Life = Life == LifeMode.Quiet ? LifeMode.Living : LifeMode.Quiet;
            activeVista?.ApplyComfort(Life, Motion);
            SaveAndNotify();
        }

        public void ToggleMotionMode()
        {
            Motion = Motion == MotionMode.Still ? MotionMode.Drift : MotionMode.Still;
            activeVista?.ApplyComfort(Life, Motion);
            SaveAndNotify();
        }

        private void SelectVista(int nextIndex)
        {
            if (nextIndex == activeIndex || nextIndex < 0 || nextIndex >= vistas.Length)
            {
                return;
            }

            void Swap()
            {
                activeVista?.Exit();
                activeIndex = nextIndex;
                activeVista = vistas[activeIndex];
                activeVista.gameObject.SetActive(true);
                activeVista.Enter(Life, Motion);
                SaveAndNotify();
            }

            if (screenFader == null || !screenFader.TryBlackout(Swap))
            {
                Swap();
            }
        }

        private int FindVista(string id)
        {
            for (var i = 0; i < vistas.Length; i++)
            {
                if (vistas[i] != null && vistas[i].VistaId == id)
                {
                    return i;
                }
            }

            return 0;
        }

        private void SaveAndNotify()
        {
            if (activeVista != null)
            {
                QuietWatchSettings.Save(activeVista.VistaId, Life, Motion);
                Debug.Log($"Quiet Watch: {activeVista.DisplayName} / {Life} / {Motion}");
            }

            StateChanged?.Invoke();
        }
    }
}
