using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Contract for a complete exterior environment. A vista owns its visual
    /// root, environmental light, sound response, comfort behaviour, and
    /// authored timing. Deactivation must leave no active vista state behind.
    /// </summary>
    public abstract class VistaEnvironment : MonoBehaviour
    {
        [SerializeField] private string vistaId = "first-question";
        [SerializeField] private string displayName = "THE FIRST QUESTION";
        [SerializeField] private string subtitle = "STARS ONLY";

        public string VistaId => vistaId;
        public string DisplayName => displayName;
        public string Subtitle => subtitle;

        public void ConfigureIdentity(string id, string title, string description)
        {
            vistaId = id;
            displayName = title;
            subtitle = description;
        }

        public abstract void Enter(LifeMode lifeMode, MotionMode motionMode);
        public abstract void ApplyComfort(LifeMode lifeMode, MotionMode motionMode);
        public abstract void Exit();
    }
}
