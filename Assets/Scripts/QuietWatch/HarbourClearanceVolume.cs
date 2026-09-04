using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Conservative station keep-out sphere used only for route audit.</summary>
    public sealed class HarbourClearanceVolume : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 1f;
        [SerializeField] private string volumeLabel = "Station structure";

        public float Radius => radius;
        public string VolumeLabel => volumeLabel;

        public void Configure(string label, float clearance)
        {
            volumeLabel = string.IsNullOrWhiteSpace(label) ? name : label;
            radius = Mathf.Max(0.1f, clearance);
        }
    }
}
