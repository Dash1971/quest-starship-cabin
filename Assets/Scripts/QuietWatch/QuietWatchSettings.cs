using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    public enum LifeMode
    {
        Quiet,
        Living
    }

    public enum MotionMode
    {
        Still,
        Drift
    }

    /// <summary>
    /// The deliberately small set of choices exposed by The Quiet Watch.
    /// Stored locally so the cabin returns to the officer's last configuration.
    /// </summary>
    public static class QuietWatchSettings
    {
        private const string VistaKey = "quiet-watch.vista";
        private const string LifeKey = "quiet-watch.life";
        private const string MotionKey = "quiet-watch.motion";

        public static string LoadVista(string fallback)
        {
            return PlayerPrefs.GetString(VistaKey, fallback);
        }

        public static LifeMode LoadLife()
        {
            return (LifeMode)Mathf.Clamp(PlayerPrefs.GetInt(LifeKey, 0), 0, 1);
        }

        public static MotionMode LoadMotion()
        {
            return (MotionMode)Mathf.Clamp(PlayerPrefs.GetInt(MotionKey, 0), 0, 1);
        }

        public static void Save(string vistaId, LifeMode life, MotionMode motion)
        {
            PlayerPrefs.SetString(VistaKey, vistaId);
            PlayerPrefs.SetInt(LifeKey, (int)life);
            PlayerPrefs.SetInt(MotionKey, (int)motion);
            PlayerPrefs.Save();
        }
    }
}
